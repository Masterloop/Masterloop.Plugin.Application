// Masterloop auto-generated template export.
// Created at: Fri, 08 Jan 2021 13:04:33 GMT
// https://portal.masterloop.net/#/templates/details/view/MLTEST

namespace Masterloop.MLTEST.Constants
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
		/// Test observation of type statistics [undefined]
		public const int StatisticsTest = 7;
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
		/// IsEnabled [Boolean] 
		public const int IsEnabled = 6;
		/// PublishIntervalSeconds [Integer] 
		public const int PublishIntervalSeconds = 7;
		/// PulseIntervalSeconds [Integer] 
		public const int PulseIntervalSeconds = 8;
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
		/// Download device settings
		public const int DownloadSettings = 1;

		/// Multicommand
		public const int Multi = 2;

		public static class MultiArguments
		{
			///DblVal [Double]
			public const int DblVal = 2;
			///IntVal [Integer]
			public const int IntVal = 3;
			///PosVal [Position]
			public const int PosVal = 4;
			///StrVal [String]
			public const int StrVal = 5;
			///StatVal [undefined]
			public const int StatVal = 6;
		}

		/// Simple command
		public const int Simple = 3;

		/// Poll all observations
		public const int PollAll = 4;

		/// Poll single observation
		public const int PollSingle = 5;

		public static class PollSingleArguments
		{
			///ObsId [Integer]
			public const int ObsId = 1;
		}

	}

}

