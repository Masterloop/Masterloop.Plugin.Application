// Masterloop auto-generated template export.
// Created at: Mon, 29 Jun 2020 10:43:47 GMT
// https://portal.masterloop.net/#/templates/details/view/MLTEST

namespace Masterloop.Templates.MLTEST
{
	public class Observations
	{
		/// Dummy value of integer type. [Integer]
		public const int Dummy = 1;
		/// Test observation of type bool [Boolean]
		public const int BoolTest = 2;
		/// Test observation of type double [Double]
		public const int DoubleTest = 3;
		/// Test observation of type integer [Integer]
		public const int IntegerTest = 4;
		/// Test observation of type position [Position]
		public const int PositionTest = 5;
		/// Test observation of type string [String]
		public const int StringTest = 6;
	}

	public class Settings
	{
		/// Owner [String] 
		public const int Owner = 1;
		/// AutoStartOnPower [Boolean] 
		public const int AutoStartOnPower = 2;
		/// CalibrationFactor [Double] 
		public const int CalibrationFactor = 3;
		/// SequenceNumber [Integer] 
		public const int SequenceNumber = 4;
		/// Location [Position] 
		public const int Location = 5;
		/// IsDisabled [Boolean] 
		public const int IsDisabled = 6;
	}

	public class Pulses
	{
		/// Device Pulse
		public const int DevicePulse = 0;
		/// Application Pulse
		public const int ApplicationPulse = 1;
	}

	public class Commands
	{
		/// Reboot charger
		public const int Reboot = 1;

		public static class RebootArguments
		{
			///Delay [Double]
			public const int Delay = 0;
			///Enter Safe Mode [Boolean]
			public const int EnterSafeMode = 1;
		}

		/// Multicommand
		public const int Multi = 2;

		public static class MultiArguments
		{
			///BoolVal [Boolean]
			public const int BoolVal = 1;
			///DblVal [Double]
			public const int DblVal = 2;
			///IntVal [Integer]
			public const int IntVal = 3;
			///PosVal [Position]
			public const int PosVal = 4;
			///StrVal [String]
			public const int StrVal = 5;
		}

		/// Simple command
		public const int Simple = 3;

	}

}

