﻿//*********************************************************************
//xCAD
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Xarial.XCad.SolidWorks.Documents.Exceptions
{
    public class ComponentNotLoadedException : Exception
    {
        public ComponentNotLoadedException(string name) : base($"Component '{name}' document is not loaded. This may be due to the component being suppressed or ilghtweight")
        {
        }
    }
}
