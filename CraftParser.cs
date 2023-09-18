using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace FFXIV_Craft_Parser_Plugin
{
    public class CraftParser : IActPluginV1
    {
        private const string PluginTitleEN = "FFXIV_Craft_Parser_Plugin";
        private const string PluginTitleCN = "FFXIV生产日志解析";
        private const int OP_CRAFT_STATUS = 0x3B9; // ? 1B5
        private const int OP_CRAFT_STEP = 0x20E;

        private TabPage pluginScreenSpace;
        private Label pluginStatusText;
        private FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivPlugin;

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            this.pluginScreenSpace = pluginScreenSpace;
            this.pluginStatusText = pluginStatusText;
            pluginStatusText.Text = $"{PluginTitleEN} / {PluginTitleCN}\n插件已启动";
            pluginScreenSpace.Text = PluginTitleCN;

            IActPluginV1 tmp = (
                from pluginData in ActGlobals.oFormActMain.ActPlugins
                where pluginData.pluginObj is FFXIV_ACT_Plugin.FFXIV_ACT_Plugin
                select pluginData.pluginObj
            ).FirstOrDefault();
            if (tmp != null) ffxivPlugin = (FFXIV_ACT_Plugin.FFXIV_ACT_Plugin)tmp;
            else throw new Exception("找不到FFXIV解析插件");
            ffxivPlugin.DataSubscription.NetworkReceived += FFXIVPlugin_NetworkReceived;
        }

        public void DeInitPlugin()
        {
            pluginStatusText.Text = $"{PluginTitleEN} / {PluginTitleCN}\n插件已退出";
            ffxivPlugin.DataSubscription.NetworkReceived -= FFXIVPlugin_NetworkReceived;
            ffxivPlugin = null;
        }

        private void FFXIVPlugin_NetworkReceived(string connection, long epoch, byte[] message)
        {
            try
            {
                var p = new Packet() { data = message };
                switch (p.Id)
                {
                    case OP_CRAFT_STATUS:
                        // TODO
                        break;
                    case OP_CRAFT_STEP:
                        unsafe
                        {
                            if (p.data.Length < sizeof(CraftStep)) return;
                            fixed (byte* ptr = p.data)
                            {
                                var craft = (CraftStep*)ptr;
                                Log("action", $"{craft->ActionID},{craft->Progress},{craft->Quality},{craft->Durability},{craft->Condition},{craft->IsSuccessed}");
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ActGlobals.oFormActMain.WriteDebugLog(e.ToString());
#endif
            }
        }

        public class Packet
        {
            public byte[] data;
            public int Id
            {
                get
                {
                    unsafe
                    {
                        if (data.Length < sizeof(ServerMessageHeader))
                            return 0;
                        fixed (byte* ptr = data)
                        {
                            var header = (ServerMessageHeader*)ptr;
                            return header->MessageType;
                        }
                    }
                }
            }

            public override string ToString() => $"[{Id:X}] len: {data.Length}";
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct ServerMessageHeader
        {
            [FieldOffset(0)]
            public uint MessageLength;
            [FieldOffset(4)]
            public uint ActorID;
            [FieldOffset(8)]
            public uint LoginUserID;
            [FieldOffset(12)]
            public uint Unknown1;
            [FieldOffset(16)]
            public ushort Unknown2;
            [FieldOffset(18)]
            public ushort MessageType;
            [FieldOffset(20)]
            public uint Unknown3;
            [FieldOffset(24)]
            public uint Seconds;
            [FieldOffset(28)]
            public uint Unknown4;
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct CraftStep
        {
            [FieldOffset(0)]
            public ServerMessageHeader header;
            [FieldOffset(0x4C)]
            public int ActionID;
            [FieldOffset(0x54)]
            public int Step;
            [FieldOffset(0x58)]
            public int Progress;
            [FieldOffset(0x5C)]
            public int ProgressAddon;
            [FieldOffset(0x60)]
            public int Quality;
            [FieldOffset(0x64)]
            public int QualityAddon;
            [FieldOffset(0x6C)]
            public int Durability;
            [FieldOffset(0x74)]
            public int Condition;
            [FieldOffset(0x7C)]
            public short Flag;

            public bool IsSuccessed { get { return (Flag & 0x10) != 0; } }

            public override string ToString() => $"Step {{ action: {ActionID}, pg:{Progress}, qu:{Quality}, du:{Durability}, co:{Condition}, su:{IsSuccessed} }}";
        }

        //偷大佬的
        //偷獭爹的
        private void Log(string type, string message)
        {
            string text = $"00|{DateTime.Now:O}|1|tnze.FFXIV_Craft_Parser_Plugin.{type}:{message}|";  //解析插件数据格式化
            ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, $"{text}"); //插入ACT日志
        }
    }
}
