//------------------------------------------------------------------------------
// <copyright file="IntelliFind.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace IntelliFind
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("94fd8fe2-2333-4bef-a9ec-58bb97ff85cf")]
    public class IntelliFindWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntelliFindWindow"/> class.
        /// </summary>
        public IntelliFindWindow() : base(null)
        {
            this.Caption = "IntelliFind";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new IntelliFindControl();
        }
    }
}
