using IIoTPlatform_E2E_Tests;
using IIoTPlatform_E2E_Tests.TestExtensions;
using System.Runtime.InteropServices;
using Xunit;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties


// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("275eb2f6-3c8c-42e3-b3a7-d793b0363134")]

// deactivate run of test in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]