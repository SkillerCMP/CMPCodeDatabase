// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/PatchLogForm.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System; using System.IO; using System.Windows.Forms; using CMPCodeDatabase.Patching; namespace CMPCodeDatabase { public partial class PatchLogForm : Form, IPatchLogSink { TextBox _txt=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,Dock=DockStyle.Fill,WordWrap=false}; public PatchLogForm(){ Text="Patch Log"; Width=900; Height=600; StartPosition=FormStartPosition.CenterParent; var panel=new FlowLayoutPanel{Dock=DockStyle.Top,Height=36,Padding=new Padding(6),FlowDirection=FlowDirection.LeftToRight}; var btnC=new Button{Text="Clear"}; var btnS=new Button{Text="Save..."}; btnC.Click+=(s,e)=>_txt.Clear(); btnS.Click+=(s,e)=>{ using var sfd=new SaveFileDialog{Filter="Text files (*.txt)|*.txt|All files (*.*)|*.*",FileName="patch-log.txt"}; if(sfd.ShowDialog(this)==DialogResult.OK) File.WriteAllText(sfd.FileName,_txt.Text); }; panel.Controls.Add(btnC); panel.Controls.Add(btnS); Controls.Add(_txt); Controls.Add(panel);} public void Write(string t){ if(IsDisposed) return; if(InvokeRequired){ BeginInvoke(new Action<string>(Write), t); return;} _txt.AppendText(t);} public void WriteLine(string t){ Write(t); Write(Environment.NewLine);} } }