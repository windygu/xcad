﻿//*********************************************************************
//xCAD
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xarial.XCad.Enums;
using Xarial.XCad.Exceptions;
using Xarial.XCad.SolidWorks.Exceptions;
using Xarial.XCad.Structures;

namespace Xarial.XCad.SolidWorks
{
    public interface ISwMacro : IXMacro 
    {
    }

    public abstract class SwMacro : ISwMacro
    {
        protected readonly ISldWorks m_App;
        protected readonly string m_Path;

        internal SwMacro(ISldWorks app, string path)
        {
            m_App = app;
            m_Path = path;
        }

        public abstract MacroEntryPoint[] EntryPoints { get; }

        public string Path => m_Path;

        public virtual void Run(MacroEntryPoint entryPoint, MacroRunOptions_e opts)
        {
            swRunMacroOption_e swOpts = swRunMacroOption_e.swRunMacroDefault;

            switch (opts) 
            {
                case MacroRunOptions_e.Default:
                    swOpts = swRunMacroOption_e.swRunMacroDefault;
                    break;

                case MacroRunOptions_e.UnloadAfterRun:
                    swOpts = swRunMacroOption_e.swRunMacroUnloadAfterRun;
                    break;
            }

            int err;
            if (!m_App.RunMacro2(m_Path, entryPoint.ModuleName, entryPoint.ProcedureName, (int)swOpts, out err))
            {
                string errDesc = "";

                switch ((swRunMacroError_e)err) 
                {
                    case swRunMacroError_e.swRunMacroError_InvalidArg:
                        errDesc = "Invalid argument";
                        break;
                    case swRunMacroError_e.swRunMacroError_MacrosAreDisabled:
                        errDesc = "Macros are disabled";
                        break;
                    case swRunMacroError_e.swRunMacroError_NotInDesignMode:
                        errDesc = "Not in design mode";
                        break;
                    case swRunMacroError_e.swRunMacroError_OnlyCodeModules:
                        errDesc = "Only code modules";
                        break;
                    case swRunMacroError_e.swRunMacroError_OutOfMemory:
                        errDesc = "Out of memory";
                        break;
                    case swRunMacroError_e.swRunMacroError_InvalidProcname:
                        errDesc = "Invalid procedure name";
                        break;
                    case swRunMacroError_e.swRunMacroError_InvalidPropertyType:
                        errDesc = "Invalid property type";
                        break;
                    case swRunMacroError_e.swRunMacroError_SuborfuncExpected:
                        errDesc = "Sub or function expected";
                        break;
                    case swRunMacroError_e.swRunMacroError_BadParmCount:
                        errDesc = "Bad parameter count";
                        break;
                    case swRunMacroError_e.swRunMacroError_BadVarType:
                        errDesc = "Bad variable type";
                        break;
                    case swRunMacroError_e.swRunMacroError_UserInterrupt:
                        throw new MacroUserInterruptException(m_Path, err);
                    case swRunMacroError_e.swRunMacroError_Exception:
                        errDesc = "Exception";
                        break;
                    case swRunMacroError_e.swRunMacroError_Overflow:
                        errDesc = "Overflow";
                        break;
                    case swRunMacroError_e.swRunMacroError_TypeMismatch:
                        errDesc = "Type mismatch";
                        break;
                    case swRunMacroError_e.swRunMacroError_ParmNotOptional:
                        errDesc = "Parameter not optional";
                        break;
                    case swRunMacroError_e.swRunMacroError_UnknownLcid:
                        errDesc = "Unknown LCID";
                        break;
                    case swRunMacroError_e.swRunMacroError_Busy:
                        errDesc = "Busy";
                        break;
                    case swRunMacroError_e.swRunMacroError_ConnectionTerminated:
                        errDesc = "Connection terminated";
                        break;
                    case swRunMacroError_e.swRunMacroError_CallRejected:
                        errDesc = "Call rejected";
                        break;
                    case swRunMacroError_e.swRunMacroError_CallFailed:
                        errDesc = "Call failed";
                        break;
                    case swRunMacroError_e.swRunMacroError_Zombied:
                        errDesc = "Zombied";
                        break;
                    case swRunMacroError_e.swRunMacroError_Invalidindex:
                        errDesc = "Invalid index";
                        break;
                    case swRunMacroError_e.swRunMacroError_NoPermission:
                        errDesc = "No permission";
                        break;
                    case swRunMacroError_e.swRunMacroError_Reverted:
                        errDesc = "Reverted";
                        break;
                    case swRunMacroError_e.swRunMacroError_TooManyOpenFiles:
                        errDesc = "Too many open files";
                        break;
                    case swRunMacroError_e.swRunMacroError_DiskError:
                        errDesc = "Disk error";
                        break;
                    case swRunMacroError_e.swRunMacroError_CantSave:
                        errDesc = "Cannot save";
                        break;
                    case swRunMacroError_e.swRunMacroError_OpenFileFailed:
                        errDesc = "Open file failed";
                        break;
                }

                throw new MacroRunFailedException(m_Path, err, errDesc);
            }
        }
    }

    public interface ISwVbaMacro : ISwMacro
    {
    }

    internal class SwVbaMacro : SwMacro, ISwVbaMacro
    {
        private class MacroEntryPointComparer : IComparer<MacroEntryPoint>
        {
            private const string MAIN_NAME = "main";

            public int Compare(MacroEntryPoint x, MacroEntryPoint y)
            {
                if (object.ReferenceEquals(x, y) || x == null || y == null)
                {
                    return 0;
                }

                if (string.Equals(x.ProcedureName, MAIN_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    return -1;
                }
                else if (string.Equals(y.ProcedureName, MAIN_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private class MacroEntryPointEqualityComparer : IEqualityComparer<MacroEntryPoint>
        {
            public bool Equals(MacroEntryPoint x, MacroEntryPoint y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }
                
                return string.Equals(x.ModuleName, y.ModuleName, StringComparison.CurrentCultureIgnoreCase)
                    && string.Equals(x.ProcedureName, y.ProcedureName, StringComparison.CurrentCultureIgnoreCase);
            }

            public int GetHashCode(MacroEntryPoint obj)
            {
                return 0;
            }
        }

        public override MacroEntryPoint[] EntryPoints { get; }

        internal SwVbaMacro(ISldWorks app, string path) : base(app, path)
        {
            EntryPoints = GetEntryPoints();
        }
        
        public override void Run(MacroEntryPoint entryPoint, MacroRunOptions_e options)
        {
            if (EntryPoints.Contains(entryPoint, new MacroEntryPointEqualityComparer()))
            {
                base.Run(entryPoint, options);
            }
            else 
            {
                throw new Exception($"Entry point '{entryPoint}' is not available in the macro '{m_Path}'");
            }
        }
        
        private MacroEntryPoint[] GetEntryPoints()
        {
            var methods = m_App.GetMacroMethods(m_Path,
                (int)swMacroMethods_e.swMethodsWithoutArguments) as string[];

            if (methods != null)
            {
                return methods.Select(m =>
                {
                    var ep = m.Split('.');
                    return new MacroEntryPoint(ep[0], ep[1]);
                }).OrderBy(e => e, new MacroEntryPointComparer()).ToArray();
            }

            return null;
        }
    }

    public interface ISwVstaMacro : ISwMacro
    {
    }

    internal class SwVstaMacro : SwMacro, ISwVstaMacro
    {
        internal SwVstaMacro(ISldWorks app, string path) : base(app, path)
        {
            EntryPoints = new MacroEntryPoint[] { new MacroEntryPoint("", "Main") };
        }

        public override MacroEntryPoint[] EntryPoints { get; }

        public override void Run(MacroEntryPoint entryPoint, MacroRunOptions_e opts)
        {
            var stopDebugVstaFlag = m_App.GetUserPreferenceToggle((int)swUserPreferenceToggle_e.swStopDebuggingVstaOnExit);

            m_App.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swStopDebuggingVstaOnExit, opts == MacroRunOptions_e.UnloadAfterRun);

            try
            {
                base.Run(entryPoint, MacroRunOptions_e.Default);
            }
            finally 
            {
                m_App.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swStopDebuggingVstaOnExit, stopDebugVstaFlag);
            }
        }
    }
}
