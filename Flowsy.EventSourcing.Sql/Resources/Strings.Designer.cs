﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Flowsy.EventSourcing.Sql.Resources {
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("Flowsy.EventSourcing.Sql.Resources.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string ConfigurationNotFoundForType {
            get {
                return ResourceManager.GetString("ConfigurationNotFoundForType", resourceCulture);
            }
        }
        
        internal static string EventRecordVersionNotSupported {
            get {
                return ResourceManager.GetString("EventRecordVersionNotSupported", resourceCulture);
            }
        }
        
        internal static string EventRecordVersionNotSpecified {
            get {
                return ResourceManager.GetString("EventRecordVersionNotSpecified", resourceCulture);
            }
        }
        
        internal static string EventMetadataNotFound {
            get {
                return ResourceManager.GetString("EventMetadataNotFound", resourceCulture);
            }
        }
        
        internal static string CouldNotReadEventMetadata {
            get {
                return ResourceManager.GetString("CouldNotReadEventMetadata", resourceCulture);
            }
        }
        
        internal static string CouldNotReadEventPayload {
            get {
                return ResourceManager.GetString("CouldNotReadEventPayload", resourceCulture);
            }
        }
        
        internal static string TypeNotFound {
            get {
                return ResourceManager.GetString("TypeNotFound", resourceCulture);
            }
        }
        
        internal static string EventPayloadNotFound {
            get {
                return ResourceManager.GetString("EventPayloadNotFound", resourceCulture);
            }
        }
        
        internal static string InvalidEventTimestamp {
            get {
                return ResourceManager.GetString("InvalidEventTimestamp", resourceCulture);
            }
        }
        
        internal static string PersistenceOperationActiveMustBeCompleted {
            get {
                return ResourceManager.GetString("PersistenceOperationActiveMustBeCompleted", resourceCulture);
            }
        }
        
        internal static string PersistenceOperationMustBeStartedAndEventsMustBeStored {
            get {
                return ResourceManager.GetString("PersistenceOperationMustBeStartedAndEventsMustBeStored", resourceCulture);
            }
        }
    }
}
