﻿//------------------------------------------------------------------------------
// <auto-generated>
//     이 코드는 도구를 사용하여 생성되었습니다.
//     런타임 버전:4.0.30319.42000
//
//     파일 내용을 변경하면 잘못된 동작이 발생할 수 있으며, 코드를 다시 생성하면
//     이러한 변경 내용이 손실됩니다.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GameServer {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.11.0.0")]
    internal sealed partial class GameServerSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static GameServerSettings defaultInstance = ((GameServerSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new GameServerSettings())));
        
        public static GameServerSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int LoginServerAcceptCount {
            get {
                return ((int)(this["LoginServerAcceptCount"]));
            }
            set {
                this["LoginServerAcceptCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("12477")]
        public int LoginServerAcceptPort {
            get {
                return ((int)(this["LoginServerAcceptPort"]));
            }
            set {
                this["LoginServerAcceptPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("127.0.0.1")]
        public string LoginServerIPAddress {
            get {
                return ((string)(this["LoginServerIPAddress"]));
            }
            set {
                this["LoginServerIPAddress"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("\\GameServerLog")]
        public string LogDirectory {
            get {
                return ((string)(this["LogDirectory"]));
            }
            set {
                this["LogDirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int DBServerConnectCount {
            get {
                return ((int)(this["DBServerConnectCount"]));
            }
            set {
                this["DBServerConnectCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("12476")]
        public int DBServerConnectPort {
            get {
                return ((int)(this["DBServerConnectPort"]));
            }
            set {
                this["DBServerConnectPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("127.0.0.1")]
        public string DBServerIPAdress {
            get {
                return ((string)(this["DBServerIPAdress"]));
            }
            set {
                this["DBServerIPAdress"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("12474")]
        public int ClientAcceptPort {
            get {
                return ((int)(this["ClientAcceptPort"]));
            }
            set {
                this["ClientAcceptPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-1")]
        public int ClientAcceptCount {
            get {
                return ((int)(this["ClientAcceptCount"]));
            }
            set {
                this["ClientAcceptCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.0.0.0")]
        public string ClientAcceptIPAdress {
            get {
                return ((string)(this["ClientAcceptIPAdress"]));
            }
            set {
                this["ClientAcceptIPAdress"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int ReadySocketCount {
            get {
                return ((int)(this["ReadySocketCount"]));
            }
            set {
                this["ReadySocketCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int MaxSocketCountPerGroup {
            get {
                return ((int)(this["MaxSocketCountPerGroup"]));
            }
            set {
                this["MaxSocketCountPerGroup"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\ProjectKJ\\GameServer\\ProjectKJServers\\Resource")]
        public string ResourceDicrectory {
            get {
                return ((string)(this["ResourceDicrectory"]));
            }
            set {
                this["ResourceDicrectory"] = value;
            }
        }
    }
}
