using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace HotseatLauncher
{
    static class Injection
    {
        public static void StartGame(Session session, Action OnExit)
        {
            if (Process.GetProcessesByName("RomeTW").Length > 0)
            {
                Debug.ShowWarning("Rome is already running!");
                return;
            }

            if (session == null || OnExit == null || session.Installation == null)
                return;

            FactionInfo currentFaction = session.GetCurrentFaction();
            if (currentFaction == null)
                return;

            if (currentFaction != session.GetPlayerFaction())
            {
                Debug.ShowWarning("It's not the player's turn!");
                return;
            }

            CodeVersion version = session.Version;
            Process.Start(new ProcessStartInfo(version.ProcessStartPath ?? session.Installation.FilePath));

            Process proc;
            long startTicks = DateTime.UtcNow.Ticks;
            while (true)
            {
                var arr = Process.GetProcessesByName("RomeTW");
                if (arr.Length > 0)
                {
                    proc = arr[0];
                    break;
                }

                if (DateTime.UtcNow.Ticks - startTicks > 10 * TimeSpan.TicksPerSecond)
                {
                    Debug.ShowWarning("Failed to start Rome!");
                    return;
                }

                System.Threading.Thread.Sleep(10);
            }

            SuspendProcess(proc);

            proc.Exited += (s, e) => Application.Current.Dispatcher.Invoke(OnExit);
            proc.EnableRaisingEvents = true;

            EditSettings(proc, version, session);
            EditFactionTurnStart(proc, version, session);
            if (session.GetLastPlayedFaction() == null) // new campaign
            {
                EditPlayableFactions(proc, version, session);
                EditStartNewCampaign(proc, version, session);
            }
            else
            {
                EditLoadGame(proc, version, session);
                EditFinishedLoading(proc, version, session);
            }

            //System.Windows.MessageBox.Show("");

            ResumeProcess(proc);
        }

        static void EditSettings(Process proc, CodeVersion version, Session session)
        {
            using (ProcessStream ps = new ProcessStream(proc))
            {
                ps.Write(0xB8); // mov eax, bool
                ps.WriteInt(Settings.ShowErrors ? 1 : 0);
                ps.WriteNops(6);
                ps.Inject(version.ShowErrAddress);

                ps.Reset();
                ps.Write(0xB8); // mov eax, bool
                ps.WriteInt(Settings.WindowedMode ? 1 : 0);
                ps.WriteNops(6);
                ps.Inject(version.WindowedAddress);

                ps.Reset();
                ps.Write(0xB8); // mov eax, bool
                ps.WriteInt(Settings.DisableAudio ? 1 : 0);
                ps.WriteNops(6);
                ps.Inject(version.NoAudioAddress);

                // set mod folder
                ps.Reset();
                ps.Write(0xB8); // mov eax, modNameAddress
                ps.WriteInt(string.Equals(session.ModName, "Default", StringComparison.OrdinalIgnoreCase) ? 0 : ps.AllocString(session.ModName + '\0'));
                ps.WriteNops(6);
                ps.Inject(version.ModAddress);
            }
        }

        static void EditPlayableFactions(Process proc, CodeVersion version, Session session)
        {
            int playableBits = 0;
            foreach (var faction in session.GetFreeFactions().Concat(session.GetTakenFactions()))
                playableBits |= (1 << faction.RomeIndex);

            int nonPlayableBits = -1 & ~playableBits;

            using (ProcessStream ps = new ProcessStream(proc))
            {
                ps.Write(0x83, 0x65, 0xCC, 0x00); // and [EBP-34], 0 
                ps.Write(0x83, 0x65, 0xA8, 0x00); // and [EBP-58], 0 
                ps.Write(0xC7, 0x45, 0xF4); // mov [EBP-C], playableFactions bitwise
                ps.WriteInt(playableBits);
                ps.Write(0xC7, 0x45, 0xFC); // mov [EBP-4], nonPlayableFactions bitwise
                ps.WriteInt(nonPlayableBits);
                ps.Write(0xE9, 0x0F, 0x01, 0x00, 0x00); // jmp to after nonPlayables read call
                ps.Inject(version.UnlockFactionsAddress);
            }
        }

        static void EditStartNewCampaign(Process proc, CodeVersion version, Session session)
        {
            // set campaign
            using (ProcessStream ps = new ProcessStream(proc))
            {
                ps.Write(0xB8); // mov eax, campaignNameAddress
                ps.WriteInt(ps.AllocString(session.CampaignName + '\0'));
                ps.WriteNops(6);
                ps.Inject(version.CampaignAddress);
            }

            proc.WriteInt(version.FactionAddress, session.GetCurrentFaction().RomeIndex);

            proc.WriteInt(version.CampaignDifficulty, session.Difficulty); // campaign difficulty
            proc.WriteInt(version.BattleDifficulty, session.Difficulty); // battle difficulty

            proc.Write(version.ArcadeBattles, 0); // arcade battles
            proc.Write(version.AutoManage, 0); // auto manage
            proc.Write(version.NoBattleTimeLimit, 0); // no battle time limit
            proc.Write(version.ShortCampaign, 0); // short campaign
        }

        static void EditFactionTurnStart(Process proc, CodeVersion version, Session session)
        {
            proc.Write(version.ControlFactionSelfCheckAddress, 0xEB); // make control faction ignore whether we're officially already that faction

            using (ProcessStream ps = new ProcessStream(proc))
            {
                // check current faction id
                ps.Write(0x51); // push ecx

                ps.Write(0x8B, 0x0D); // mov ecx, [018CD818]
                ps.WriteInt(0x018CD818);

                ps.Write(0x8B, 0x81); // mov eax, [ecx+230]
                ps.WriteInt(0x230); // current faction id

                int nextIndex = session.SwitchIndex(session.GetCurrentFaction().Index) - 1;

                if (session.GetLastPlayedFaction() == null)
                {
                    ps.Write(0x83, 0xF8, (byte)nextIndex); // cmp eax, nextIndex
                    ps.Write(0x74); // je to hook exit
                    ps.WriteByteDistance("hookexit");
                }
                else
                {
                    int lastIndex = session.SwitchIndex(session.GetLastPlayedFaction().Index) - 1;
                    if (lastIndex == nextIndex)
                    {
                        nextIndex += 2;
                        if (session.GetModFolder().GetFaction((byte)nextIndex) == null)
                            nextIndex = 1;

                        //if (currentIndex == nextIndex) do nothing;
                        ps.Write(0x83, 0xF8, (byte)(nextIndex-1)); // cmp eax, nextIndex
                        ps.Write(0x75); // jne to hook exit
                        ps.WriteByteDistance("hookexit");
                    }
                    else if (lastIndex < nextIndex)
                    {
                        //if (currentIndex <= nextIndex && currentIndex > lastIndex) do nothing;

                        ps.Write(0x83, 0xF8, (byte)nextIndex); // cmp eax, nextIndex
                        ps.Write(0x77); // ja to save & quit
                        ps.WriteByteDistance("savequit");

                        ps.Write(0x83, 0xF8, (byte)lastIndex); // cmp eax, lastIndex

                        ps.Write(0x77); // ja to hook exit
                        ps.WriteByteDistance("hookexit");
                    }
                    else if (lastIndex > nextIndex)
                    {
                        //if (currentIndex <= nextIndex || currentIndex > lastIndex) do nothing;

                        ps.Write(0x83, 0xF8, (byte)nextIndex); // cmp eax, nextIndex
                        ps.Write(0x76); // jna to hookexit
                        ps.WriteByteDistance("hookexit");

                        ps.Write(0x83, 0xF8, (byte)lastIndex); // cmp eax, lastIndex

                        ps.Write(0x77); // ja to hook exit
                        ps.WriteByteDistance("hookexit");
                    }

                    ps.AddByteDistance("savequit");
                }

                // save game
                ps.Write(0x6A, 0x00); // push 0

                ps.Write(0x68); // push file path string
                ps.WriteInt(ps.AllocString(session.OutputSaveGamePath, true, 2));

                ps.Write(0x8B, 0x0D, 0x28, 0xD8, 0x8C, 0x01); // mov ecx, [018CD828]

                ps.Write(0xE8); // call save
                ps.WriteRelativeAddress(0x420FB9);

                // close game
                //ps.Write(0xC6, 0x05); // mov [12CB8EC], 1 // quit, does not exit reliable between turns ????
                //ps.WriteInt(0x12CB8EC);
                //ps.Write(0x01);

                ps.Write(0x6A, 0x00); // push 0

                ps.Write(0xE8); // call exit, simply kills the process // bad
                ps.WriteRelativeAddress(0xFC5603);

                // hook exit
                ps.AddByteDistance("hookexit");

                ps.Write(0x59); // pop ecx

                //ps.Write(0x5F, 0x5E, 0x5B, 0xC9, 0xC3);

                // ori code
                ps.Write(0xE8);
                ps.WriteRelativeAddress(0x8AAE20);

                ps.Write(0xE9); // jmp back
                ps.WriteRelativeAddress(version.BetweenFactionTurnsAddress + 5);

                int hookAddr = ps.AllocInject();

                //
                // Jmp to factionstarthook
                //
                ps.Reset();

                ps.Write(0xE9);
                ps.WriteRelativeAddress(hookAddr);

                ps.Inject(version.BetweenFactionTurnsAddress);
            }
        }

        static void EditFinishedLoading(Process proc, CodeVersion version, Session session)
        {
            // set control to player faction
            using (ProcessStream ps = new ProcessStream(proc))
            {
                // ori code
                ps.Write(0xE8);
                ps.WriteRelativeAddress(0x8C0750);

                // control faction
                ps.Write(0x51); // push ecx

                if (session.GetLastPlayedFaction() != null)
                {
                    ps.Write(0xA1); // mov eax, [029219D8]
                    ps.WriteInt(0x029219D8);

                    ps.Write(0xC7, 0x00); // mov [eax], lastIndex
                    ps.WriteInt(session.SwitchIndex(session.GetLastPlayedFaction().Index) - 1);
                }

                ps.Write(0x6A, 0x00); // (push pointer 0xFFFF) // just push 0

                ps.Write(0x68); // push command string
                ps.WriteInt(ps.AllocString("control " + session.GetCurrentFaction().Name));

                ps.Write(0xB9, 0x88, 0xB6, 0x94, 0x02); // mov ecx, offset 0x0294B688

                ps.Write(0xE8); // call command interpreter
                ps.WriteRelativeAddress(0xDE1718);

                ps.Write(0x59); // pop ecx

                ps.Write(0xE9);
                ps.WriteRelativeAddress(version.FinishedLoadingAddress + 5);

                int hookAddress = ps.AllocInject();

                ps.Reset();
                ps.Write(0xE9);
                ps.WriteRelativeAddress(hookAddress);
                ps.Inject(version.FinishedLoadingAddress);
            }
        }

        static void EditLoadGame(Process proc, CodeVersion version, Session session)
        {
            using (ProcessStream ps = new ProcessStream(proc))
            {
                // load game
                ps.Write(0x68); // push file string
                ps.WriteInt(ps.AllocString(session.InputSaveGamePath, true, 2));

                ps.Write(0xE8); // call load save game
                ps.WriteRelativeAddress(0xD88D2F);

                // correct stack
                ps.Write(0x83, 0xC4, 0x04); // add esp, 4

                // call ori function
                ps.Write(0xE8);
                ps.WriteRelativeAddress(0x40F750);

                // jump back
                ps.Write(0xE9);
                ps.WriteRelativeAddress(version.StartGameAddress + 5);

                int hookAddress = ps.AllocInject();

                ps.Reset();
                ps.Write(0xE9);
                ps.WriteRelativeAddress(hookAddress);
                ps.Inject(version.StartGameAddress);
            }
        }

        /*static void EditBetweenFactionTurns(Process proc, GameSession session, FactionInfo faction)
        {
            using (ProcessStream ps = new ProcessStream())
            {
                const int betweenFactionTurns = 0x45334A;

                // check current faction id
                ps.Write(0x8B, 0x81); // mov eax, [ecx+230]
                ps.WriteInt(0x230); // current faction id

                ps.Write(0x83, 0xF8, (byte)session.GetSwitchFaction(faction).Index); // cmp eax, value

                ps.Write(0x75); // jne to hook exit
                ps.WriteByteDistance("hookexit");

                // push ecx
                ps.Write(0x51);

                // save game
                ps.Write(0x6A, 0x00); // push 0

                ps.Write(0x68); // push file path string
                ps.WriteInt(ProcessStream.AllocString(proc, session.OutputSaveGamePath, true, 2));

                ps.Write(0x8B, 0x0D, 0x28, 0xD8, 0x8C, 0x01); // mov ecx, [018CD828]

                ps.Write(0xE8); // call save
                ps.WriteRelativeAddress(0x420FB9);

                // close game
                ps.Write(0xC6, 0x05); // mov [12CB8EC], 1
                ps.WriteInt(0x12CB8EC);
                ps.Write(0x01);

                // pop ecx
                ps.Write(0x59);

                // hook exit
                ps.AddByteDistance("hookexit");

                // ori code
                ps.Write(0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x70);

                ps.Write(0xE9); // jmp back
                ps.WriteRelativeAddress(betweenFactionTurns + 6);

                int hookAddr = ps.AllocInject(proc);

                //
                // Jmp to factionstarthook
                //
                ps.Reset();

                ps.Write(0xE9);
                ps.WriteRelativeAddress(hookAddr);
                ps.Write(0x90);

                ps.Inject(proc, betweenFactionTurns);
            }
        }*/

        #region Suspend & Resume Process

        [Flags]
        enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        static void SuspendProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        static void ResumeProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                while (ResumeThread(pOpenThread) > 1)
                {
                }

                CloseHandle(pOpenThread);
            }
        }

        #endregion
    }
}
