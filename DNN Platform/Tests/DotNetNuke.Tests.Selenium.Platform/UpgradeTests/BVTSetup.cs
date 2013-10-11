﻿using DNNSelenium.Platform.Properties;
using NUnit.Framework;

namespace DNNSelenium.Platform.UpgradeTests
{
	//[SetUpFixture]
	//[Category("BVT")] 
	public class BVTSetup : Common.Tests.Upgrade.BVTSetup
	{
		protected override string DataFileLocation
		{
			get { return @"UpgradeTests\" + Settings.Default.BVTDataFile; }
		}
	}
}