﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using LCL.VSPackage.Modeling;

namespace LCL.VSPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    //[ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string)]
    [Guid(GuidList.guidVSPackagePkgString)]
    public sealed class VSPackagePackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSPackagePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
           // base.RegisterEditorFactory(new EditorFactory(this));

            AddCommands();
        }
        private void AddCommands()
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                // Create the command for the menu item.
                foreach (var type in this.GetType().Assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(Command)) && !type.IsAbstract)
                    {
                        var cmd = Activator.CreateInstance(type) as Command;

                        cmd.Package = this;
                        cmd.Initialize();
                        mcs.AddCommand(cmd.MenuCommand);
                    }
                }
            }
        }
        #endregion

        private void MenuItemCallback(object sender, EventArgs e)
        {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                uiShell.ShowMessageBox(
                    0, ref clsid,
                    "FirstPackage",
                    string.Format(CultureInfo.CurrentCulture,
                        "Inside {0}.MenuItemCallback()", this.ToString()),
                    string.Empty, 0,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO,
                    0, out result));
        }

    }
}
