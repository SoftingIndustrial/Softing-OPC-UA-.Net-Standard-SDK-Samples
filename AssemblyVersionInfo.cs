/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH.
 * All rights reserved.
 *
 * The Software is subject to the Softing Industrial Automation GmbH’s
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *
 * The Software is based on the OPC Foundation, Inc.’s software. This
 * original OPC Foundation’s software can be found here:
 * http://www.opcfoundation.org
 *
 * The original OPC Foundation’s software is subject to the OPC Foundation
 * MIT License 1.00, which can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 *
 * ======================================================================*/

using System.Reflection;

[assembly: AssemblyCopyright(AssemblyVersionInfo.Copyright)]
[assembly: AssemblyVersion(AssemblyVersionInfo.CurrentVersion)]
[assembly: AssemblyFileVersion(AssemblyVersionInfo.CurrentFileVersion)]
[assembly: AssemblyCompany(AssemblyVersionInfo.AssemblyCompany)]
[assembly: AssemblyProduct(AssemblyVersionInfo.AssemblyProduct)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

/// <summary>
/// Defines string constants for Toolkit version information.
/// </summary>
internal static class AssemblyVersionInfo
{
    /// <summary> The current copy right notice. </summary>
    public const string Copyright = "© 2020 Softing Industrial Automation GmbH";

    /// BEWARE THAT IN VS 2017 THE USAGE OF * PLACEHOLDER IS NOT ALLOWED
    /// <summary> The current build version. </summary>
    public const string CurrentVersion = "2.40.0.1871";

    /// <summary> The current build file version. </summary>
    public const string CurrentFileVersion = "2.40.0.1871";

    /// <summary>The assembly copyright owner.</summary>
    public const string AssemblyCompany = "Softing Industrial Automation GmbH";

    /// <summary>The product name.</summary>
    public const string AssemblyProduct = "dataFEED OPC UA .NET Standard SDK";

	/// <summary>The product release date.</summary>
    public const string ReleaseDate = "2020/02/03 13:51:31";
}
