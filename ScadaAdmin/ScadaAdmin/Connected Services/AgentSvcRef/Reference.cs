﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ScadaAdmin.AgentSvcRef {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ServiceApp", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    public enum ServiceApp : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Server = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Communicator = 1,
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ServiceCommand", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    public enum ServiceCommand : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Start = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Stop = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Restart = 2,
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ServiceStatus", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    public enum ServiceStatus : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Undefined = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Normal = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Stopped = 2,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Error = 3,
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.FlagsAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="ConfigParts", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    public enum ConfigParts : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        None = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Base = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Interface = 2,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Server = 4,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Comm = 8,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Web = 16,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        All = 31,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ConfigOptions", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    [System.SerializableAttribute()]
    public partial class ConfigOptions : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private ScadaAdmin.AgentSvcRef.ConfigParts ConfigPartsField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private ScadaAdmin.AgentSvcRef.RelPath[] IgnoredPathsField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public ScadaAdmin.AgentSvcRef.ConfigParts ConfigParts {
            get {
                return this.ConfigPartsField;
            }
            set {
                if ((this.ConfigPartsField.Equals(value) != true)) {
                    this.ConfigPartsField = value;
                    this.RaisePropertyChanged("ConfigParts");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public ScadaAdmin.AgentSvcRef.RelPath[] IgnoredPaths {
            get {
                return this.IgnoredPathsField;
            }
            set {
                if ((object.ReferenceEquals(this.IgnoredPathsField, value) != true)) {
                    this.IgnoredPathsField = value;
                    this.RaisePropertyChanged("IgnoredPaths");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="RelPath", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    [System.SerializableAttribute()]
    public partial class RelPath : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private ScadaAdmin.AgentSvcRef.AppFolder AppFolderField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private ScadaAdmin.AgentSvcRef.ConfigParts ConfigPartField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string PathField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public ScadaAdmin.AgentSvcRef.AppFolder AppFolder {
            get {
                return this.AppFolderField;
            }
            set {
                if ((this.AppFolderField.Equals(value) != true)) {
                    this.AppFolderField = value;
                    this.RaisePropertyChanged("AppFolder");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public ScadaAdmin.AgentSvcRef.ConfigParts ConfigPart {
            get {
                return this.ConfigPartField;
            }
            set {
                if ((this.ConfigPartField.Equals(value) != true)) {
                    this.ConfigPartField = value;
                    this.RaisePropertyChanged("ConfigPart");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Path {
            get {
                return this.PathField;
            }
            set {
                if ((object.ReferenceEquals(this.PathField, value) != true)) {
                    this.PathField = value;
                    this.RaisePropertyChanged("Path");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="AppFolder", Namespace="http://schemas.datacontract.org/2004/07/Scada.Agent")]
    public enum AppFolder : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Root = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Config = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Log = 2,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Storage = 3,
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="AgentSvcRef.AgentSvc")]
    public interface AgentSvc {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/CreateSession", ReplyAction="http://tempuri.org/AgentSvc/CreateSessionResponse")]
        bool CreateSession(out long sessionID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/Login", ReplyAction="http://tempuri.org/AgentSvc/LoginResponse")]
        bool Login(out string errMsg, long sessionID, string username, string encryptedPassword, string scadaInstanceName);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/IsLoggedOn", ReplyAction="http://tempuri.org/AgentSvc/IsLoggedOnResponse")]
        bool IsLoggedOn(long sessionID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/ControlService", ReplyAction="http://tempuri.org/AgentSvc/ControlServiceResponse")]
        bool ControlService(long sessionID, ScadaAdmin.AgentSvcRef.ServiceApp serviceApp, ScadaAdmin.AgentSvcRef.ServiceCommand command);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/GetServiceStatus", ReplyAction="http://tempuri.org/AgentSvc/GetServiceStatusResponse")]
        bool GetServiceStatus(out ScadaAdmin.AgentSvcRef.ServiceStatus status, long sessionID, ScadaAdmin.AgentSvcRef.ServiceApp serviceApp);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/GetAvailableConfig", ReplyAction="http://tempuri.org/AgentSvc/GetAvailableConfigResponse")]
        bool GetAvailableConfig(out ScadaAdmin.AgentSvcRef.ConfigParts configParts, long sessionID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/DownloadConfig", ReplyAction="http://tempuri.org/AgentSvc/DownloadConfigResponse")]
        System.IO.Stream DownloadConfig(long sessionID, ScadaAdmin.AgentSvcRef.ConfigOptions configOptions);
        
        // CODEGEN: Generating message contract since the operation UploadConfig is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/UploadConfig", ReplyAction="http://tempuri.org/AgentSvc/UploadConfigResponse")]
        ScadaAdmin.AgentSvcRef.UploadConfigResponse UploadConfig(ScadaAdmin.AgentSvcRef.ConfigUploadMessage request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/PackConfig", ReplyAction="http://tempuri.org/AgentSvc/PackConfigResponse")]
        bool PackConfig(long sessionID, string destFileName, ScadaAdmin.AgentSvcRef.ConfigOptions configOptions);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/UnpackConfig", ReplyAction="http://tempuri.org/AgentSvc/UnpackConfigResponse")]
        bool UnpackConfig(long sessionID, string srcFileName, ScadaAdmin.AgentSvcRef.ConfigOptions configOptions);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/Browse", ReplyAction="http://tempuri.org/AgentSvc/BrowseResponse")]
        bool Browse(out string[] directories, out string[] files, long sessionID, ScadaAdmin.AgentSvcRef.RelPath relPath);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/DownloadFile", ReplyAction="http://tempuri.org/AgentSvc/DownloadFileResponse")]
        System.IO.Stream DownloadFile(long sessionID, ScadaAdmin.AgentSvcRef.RelPath relPath);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/AgentSvc/DownloadFileRest", ReplyAction="http://tempuri.org/AgentSvc/DownloadFileRestResponse")]
        System.IO.Stream DownloadFileRest(long sessionID, ScadaAdmin.AgentSvcRef.RelPath relPath, long position);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="ConfigUploadMessage", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class ConfigUploadMessage {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public ScadaAdmin.AgentSvcRef.ConfigOptions ConfigOptions;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public long SessionID;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public System.IO.Stream Stream;
        
        public ConfigUploadMessage() {
        }
        
        public ConfigUploadMessage(ScadaAdmin.AgentSvcRef.ConfigOptions ConfigOptions, long SessionID, System.IO.Stream Stream) {
            this.ConfigOptions = ConfigOptions;
            this.SessionID = SessionID;
            this.Stream = Stream;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class UploadConfigResponse {
        
        public UploadConfigResponse() {
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface AgentSvcChannel : ScadaAdmin.AgentSvcRef.AgentSvc, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class AgentSvcClient : System.ServiceModel.ClientBase<ScadaAdmin.AgentSvcRef.AgentSvc>, ScadaAdmin.AgentSvcRef.AgentSvc {
        
        public AgentSvcClient() {
        }
        
        public AgentSvcClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public AgentSvcClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public AgentSvcClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public AgentSvcClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public bool CreateSession(out long sessionID) {
            return base.Channel.CreateSession(out sessionID);
        }
        
        public bool Login(out string errMsg, long sessionID, string username, string encryptedPassword, string scadaInstanceName) {
            return base.Channel.Login(out errMsg, sessionID, username, encryptedPassword, scadaInstanceName);
        }
        
        public bool IsLoggedOn(long sessionID) {
            return base.Channel.IsLoggedOn(sessionID);
        }
        
        public bool ControlService(long sessionID, ScadaAdmin.AgentSvcRef.ServiceApp serviceApp, ScadaAdmin.AgentSvcRef.ServiceCommand command) {
            return base.Channel.ControlService(sessionID, serviceApp, command);
        }
        
        public bool GetServiceStatus(out ScadaAdmin.AgentSvcRef.ServiceStatus status, long sessionID, ScadaAdmin.AgentSvcRef.ServiceApp serviceApp) {
            return base.Channel.GetServiceStatus(out status, sessionID, serviceApp);
        }
        
        public bool GetAvailableConfig(out ScadaAdmin.AgentSvcRef.ConfigParts configParts, long sessionID) {
            return base.Channel.GetAvailableConfig(out configParts, sessionID);
        }
        
        public System.IO.Stream DownloadConfig(long sessionID, ScadaAdmin.AgentSvcRef.ConfigOptions configOptions) {
            return base.Channel.DownloadConfig(sessionID, configOptions);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        ScadaAdmin.AgentSvcRef.UploadConfigResponse ScadaAdmin.AgentSvcRef.AgentSvc.UploadConfig(ScadaAdmin.AgentSvcRef.ConfigUploadMessage request) {
            return base.Channel.UploadConfig(request);
        }
        
        public void UploadConfig(ScadaAdmin.AgentSvcRef.ConfigOptions ConfigOptions, long SessionID, System.IO.Stream Stream) {
            ScadaAdmin.AgentSvcRef.ConfigUploadMessage inValue = new ScadaAdmin.AgentSvcRef.ConfigUploadMessage();
            inValue.ConfigOptions = ConfigOptions;
            inValue.SessionID = SessionID;
            inValue.Stream = Stream;
            ScadaAdmin.AgentSvcRef.UploadConfigResponse retVal = ((ScadaAdmin.AgentSvcRef.AgentSvc)(this)).UploadConfig(inValue);
        }
        
        public bool PackConfig(long sessionID, string destFileName, ScadaAdmin.AgentSvcRef.ConfigOptions configOptions) {
            return base.Channel.PackConfig(sessionID, destFileName, configOptions);
        }
        
        public bool UnpackConfig(long sessionID, string srcFileName, ScadaAdmin.AgentSvcRef.ConfigOptions configOptions) {
            return base.Channel.UnpackConfig(sessionID, srcFileName, configOptions);
        }
        
        public bool Browse(out string[] directories, out string[] files, long sessionID, ScadaAdmin.AgentSvcRef.RelPath relPath) {
            return base.Channel.Browse(out directories, out files, sessionID, relPath);
        }
        
        public System.IO.Stream DownloadFile(long sessionID, ScadaAdmin.AgentSvcRef.RelPath relPath) {
            return base.Channel.DownloadFile(sessionID, relPath);
        }
        
        public System.IO.Stream DownloadFileRest(long sessionID, ScadaAdmin.AgentSvcRef.RelPath relPath, long position) {
            return base.Channel.DownloadFileRest(sessionID, relPath, position);
        }
    }
}
