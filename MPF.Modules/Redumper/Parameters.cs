﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MPF.Core.Data;
using RedumpLib.Data;

namespace MPF.Modules.Redumper
{
    /// <summary>
    /// Represents a generic set of Redumper parameters
    /// </summary>
    public class Parameters : BaseParameters
    {
        #region Generic Dumping Information

        /// <inheritdoc/>
        public override string InputPath => DriveValue;

        /// <inheritdoc/>
        public override string OutputPath => Path.Combine(ImagePathValue ?? string.Empty, ImageNameValue ?? string.Empty);

        /// <inheritdoc/>
        public override int? Speed => SpeedValue;

        #endregion

        #region Metadata

        /// <inheritdoc/>
        public override InternalProgram InternalProgram => InternalProgram.Redumper;

        #endregion

        #region Flag Values

        /// <summary>
        /// Maximum absolute sample value to treat it as silence (default: 32)
        /// </summary>
        public int? AudioSilenceThresholdValue { get; set; }

        /// <summary>
        /// Drive to use, first available drive with disc, if not provided
        /// </summary>
        public string DriveValue { get; set; }

        /// <summary>
        /// Override offset autodetection and use supplied value
        /// </summary>
        public int? ForceOffsetValue { get; set; }

        /// <summary>
        /// Dump files prefix, autogenerated in dump mode, if not provided
        /// </summary>
        public string ImageNameValue { get; set; }

        /// <summary>
        /// Dump files base directory
        /// </summary>
        public string ImagePathValue { get; set; }

        /// <summary>
        /// Number of sector retries in case of SCSI/C2 error (default: 0)
        /// </summary>
        public int? RetriesValue { get; set; }

        /// <summary>
        /// Rings mode, maximum ring size to stop subdivision (rings, default: 1024)
        /// </summary>
        public int? RingSizeValue { get; set; }

        /// <summary>
        /// LBA ranges of sectors to skip
        /// </summary>
        public string SkipValue { get; set; }

        /// <summary>
        /// Fill byte value for skipped sectors (default: 0x55)
        /// </summary>
        public byte? SkipFillValue { get; set; }

        /// <summary>
        /// Rings mode, number of sectors to skip on SCSI error (default: 4096)
        /// </summary>
        public int? SkipSizeValue { get; set; }

        /// <summary>
        /// Drive read speed, optimal drive speed will be used if not provided
        /// </summary>
        public int? SpeedValue { get; set; }

        /// <summary>
        /// LBA to stop dumping at (everything before the value, useful for discs with fake TOC
        /// </summary>
        public int? StopLBAValue { get; set; }

        #endregion

        /// <inheritdoc/>
        public Parameters(string parameters) : base(parameters) { }

        /// <inheritdoc/>
        public Parameters(RedumpSystem? system, MediaType? type, char driveLetter, string filename, int? driveSpeed, Options options)
            : base(system, type, driveLetter, filename, driveSpeed, options)
        {
        }

        #region BaseParameters Implementations

        /// <inheritdoc/>
        public override (bool, List<string>) CheckAllOutputFilesExist(string basePath, bool preCheck)
        {
            // TODO: Fill out
            return (true, new List<string>());
        }

        /// <inheritdoc/>
        public override void GenerateSubmissionInfo(SubmissionInfo info, string basePath, Drive drive, bool includeArtifacts)
        {
            // TODO: Fill in submission info specifics for Redumper
            string outputDirectory = Path.GetDirectoryName(basePath);

            switch (this.Type)
            {
                // Determine type-specific differences
            }

            switch (this.System)
            {
                case RedumpSystem.KonamiPython2:
                    if (GetPlayStationExecutableInfo(drive?.Letter, out string pythonTwoSerial, out Region? pythonTwoRegion, out string pythonTwoDate))
                    {
                        // Ensure internal serial is pulled from local data
                        info.CommonDiscInfo.CommentsSpecialFields[SiteCode.InternalSerialName] = pythonTwoSerial ?? string.Empty;
                        info.CommonDiscInfo.Region = info.CommonDiscInfo.Region ?? pythonTwoRegion;
                        info.CommonDiscInfo.EXEDateBuildDate = pythonTwoDate;
                    }

                    info.VersionAndEditions.Version = GetPlayStation2Version(drive?.Letter) ?? "";
                    break;

                case RedumpSystem.SonyPlayStation:
                    if (GetPlayStationExecutableInfo(drive?.Letter, out string playstationSerial, out Region? playstationRegion, out string playstationDate))
                    {
                        // Ensure internal serial is pulled from local data
                        info.CommonDiscInfo.CommentsSpecialFields[SiteCode.InternalSerialName] = playstationSerial ?? string.Empty;
                        info.CommonDiscInfo.Region = info.CommonDiscInfo.Region ?? playstationRegion;
                        info.CommonDiscInfo.EXEDateBuildDate = playstationDate;
                    }

                    break;

                case RedumpSystem.SonyPlayStation2:
                    if (GetPlayStationExecutableInfo(drive?.Letter, out string playstationTwoSerial, out Region? playstationTwoRegion, out string playstationTwoDate))
                    {
                        // Ensure internal serial is pulled from local data
                        info.CommonDiscInfo.CommentsSpecialFields[SiteCode.InternalSerialName] = playstationTwoSerial ?? string.Empty;
                        info.CommonDiscInfo.Region = info.CommonDiscInfo.Region ?? playstationTwoRegion;
                        info.CommonDiscInfo.EXEDateBuildDate = playstationTwoDate;
                    }

                    info.VersionAndEditions.Version = GetPlayStation2Version(drive?.Letter) ?? "";
                    break;

                case RedumpSystem.SonyPlayStation4:
                    info.VersionAndEditions.Version = GetPlayStation4Version(drive?.Letter) ?? "";
                    break;

                case RedumpSystem.SonyPlayStation5:
                    info.VersionAndEditions.Version = GetPlayStation5Version(drive?.Letter) ?? "";
                    break;
            }
        }

        /// <inheritdoc/>
        public override string GenerateParameters()
        {
            List<string> parameters = new List<string>();

            if (BaseCommand == null)
                BaseCommand = CommandStrings.NONE;

            if (!string.IsNullOrWhiteSpace(BaseCommand))
                parameters.Add(BaseCommand);
            else
                return null;

            // Audio Silence Threshold
            if (IsFlagSupported(FlagStrings.AudioSilenceThreshold))
            {
                if (this[FlagStrings.AudioSilenceThreshold] == true)
                {
                    if (AudioSilenceThresholdValue != null && AudioSilenceThresholdValue >= 0)
                        parameters.Add($"{FlagStrings.AudioSilenceThreshold}={AudioSilenceThresholdValue}");
                    else
                        return null;
                }
            }

            // CD-i Correct Offset
            if (IsFlagSupported(FlagStrings.CDiCorrectOffset))
            {
                if (this[FlagStrings.CDiCorrectOffset] == true)
                    parameters.Add(FlagStrings.CDiCorrectOffset);
            }

            // CD-i Ready Normalize
            if (IsFlagSupported(FlagStrings.CDiReadyNormalize))
            {
                if (this[FlagStrings.CDiReadyNormalize] == true)
                    parameters.Add(FlagStrings.CDiReadyNormalize);
            }

            // Descramble New
            if (IsFlagSupported(FlagStrings.DescrambleNew))
            {
                if (this[FlagStrings.DescrambleNew] == true)
                    parameters.Add(FlagStrings.DescrambleNew);
            }

            // Drive
            if (IsFlagSupported(FlagStrings.Drive))
            {
                if (this[FlagStrings.Drive] == true)
                {
                    if (DriveValue != null)
                        parameters.Add($"{FlagStrings.Drive}={DriveValue}");
                    else
                        return null;
                }
            }

            // ForceOffset
            if (IsFlagSupported(FlagStrings.ForceOffset))
            {
                if (this[FlagStrings.ForceOffset] == true)
                {
                    if (ForceOffsetValue != null)
                        parameters.Add($"{FlagStrings.ForceOffset}={ForceOffsetValue}");
                    else
                        return null;
                }
            }

            // Force QTOC
            if (IsFlagSupported(FlagStrings.ForceQTOC))
            {
                if (this[FlagStrings.ForceQTOC] == true)
                    parameters.Add(FlagStrings.ForceQTOC);
            }

            // Force Split
            if (IsFlagSupported(FlagStrings.ForceSplit))
            {
                if (this[FlagStrings.ForceSplit] == true)
                    parameters.Add(FlagStrings.ForceSplit);
            }

            // Force TOC
            if (IsFlagSupported(FlagStrings.ForceTOC))
            {
                if (this[FlagStrings.ForceTOC] == true)
                    parameters.Add(FlagStrings.ForceTOC);
            }

            // Help
            if (IsFlagSupported(FlagStrings.HelpLong))
            {
                if (this[FlagStrings.HelpLong] == true)
                    parameters.Add(FlagStrings.HelpLong);
            }

            // ISO9660 Trim
            if (IsFlagSupported(FlagStrings.ISO9660Trim))
            {
                if (this[FlagStrings.ISO9660Trim] == true)
                    parameters.Add(FlagStrings.ISO9660Trim);
            }

            // Image Name
            if (IsFlagSupported(FlagStrings.ImageName))
            {
                if (this[FlagStrings.ImageName] == true)
                {
                    if (!string.IsNullOrWhiteSpace(ImageNameValue))
                        parameters.Add($"{FlagStrings.ImageName}={ImageNameValue}");
                    else
                        return null;
                }
            }

            // Image Path
            if (IsFlagSupported(FlagStrings.ImagePath))
            {
                if (this[FlagStrings.ImagePath] == true)
                {
                    if (!string.IsNullOrWhiteSpace(ImagePathValue))
                        parameters.Add($"{FlagStrings.ImagePath}={ImagePathValue}");
                    else
                        return null;
                }
            }

            // Leave Unchanged
            if (IsFlagSupported(FlagStrings.LeaveUnchanged))
            {
                if (this[FlagStrings.LeaveUnchanged] == true)
                    parameters.Add(FlagStrings.LeaveUnchanged);
            }

            // Overwrite
            if (IsFlagSupported(FlagStrings.Overwrite))
            {
                if (this[FlagStrings.Overwrite] == true)
                    parameters.Add(FlagStrings.Overwrite);
            }

            // Refine Subchannel
            if (IsFlagSupported(FlagStrings.RefineSubchannel))
            {
                if (this[FlagStrings.RefineSubchannel] == true)
                    parameters.Add(FlagStrings.RefineSubchannel);
            }

            // Retries
            if (IsFlagSupported(FlagStrings.Retries))
            {
                if (this[FlagStrings.Retries] == true)
                {
                    if (RetriesValue != null && RetriesValue >= 0)
                        parameters.Add($"{FlagStrings.Retries}={RetriesValue}");
                    else
                        return null;
                }
            }

            // Ring Size
            if (IsFlagSupported(FlagStrings.RingSize))
            {
                if (this[FlagStrings.RingSize] == true)
                {
                    if (RingSizeValue != null && RingSizeValue >= 0)
                        parameters.Add($"{FlagStrings.RingSize}={RingSizeValue}");
                    else
                        return null;
                }
            }

            // Skip
            if (IsFlagSupported(FlagStrings.Skip))
            {
                if (this[FlagStrings.Skip] == true)
                {
                    if (SkipValue != null && SkipValue >= 0))
                        parameters.Add($"{FlagStrings.Skip}={SkipValue}");
                    else
                        return null;
                }
            }

            // Skip Fill
            if (IsFlagSupported(FlagStrings.SkipFill))
            {
                if (this[FlagStrings.SkipFill] == true)
                {
                    if (SkipFillValue != null && SkipFillValue >= 0)
                        parameters.Add($"{FlagStrings.SkipFill}={SkipFillValue:x}");
                    else
                        return null;
                }
            }

            // Skip Lead-In
            if (IsFlagSupported(FlagStrings.SkipLeadIn))
            {
                if (this[FlagStrings.SkipLeadIn] == true)
                    parameters.Add(FlagStrings.SkipLeadIn);
            }

            // Skip Size
            if (IsFlagSupported(FlagStrings.SkipSize))
            {
                if (this[FlagStrings.SkipSize] == true)
                {
                    if (SkipSizeValue != null && SkipSizeValue >= 0)
                        parameters.Add($"{FlagStrings.SkipSize}={SkipSizeValue}");
                    else
                        return null;
                }
            }

            // Speed
            if (IsFlagSupported(FlagStrings.Speed))
            {
                if (this[FlagStrings.Speed] == true)
                {
                    if (SpeedValue != null && SkipSizeValue >= 1)
                        parameters.Add($"{FlagStrings.Speed}={SpeedValue}");
                    else
                        return null;
                }
            }

            // Stop LBA
            if (IsFlagSupported(FlagStrings.StopLBA))
            {
                if (this[FlagStrings.StopLBA] == true)
                {
                    if (StopLBAValue != null)
                        parameters.Add($"{FlagStrings.StopLBA}={StopLBAValue}");
                    else
                        return null;
                }
            }

            // Unsupported
            if (IsFlagSupported(FlagStrings.Unsupported))
            {
                if (this[FlagStrings.Unsupported] == true)
                    parameters.Add(FlagStrings.Unsupported);
            }

            // Verbose
            if (IsFlagSupported(FlagStrings.Verbose))
            {
                if (this[FlagStrings.Verbose] == true)
                    parameters.Add(FlagStrings.Verbose);
            }

            return string.Join(" ", parameters);
        }

        /// <inheritdoc/>
        public override Dictionary<string, List<string>> GetCommandSupport()
        {
            // TODO: Figure out actual support for each flag
            return new Dictionary<string, List<string>>()
            {
                [CommandStrings.NONE] = new List<string>()
                {
                    FlagStrings.HelpLong,
                    FlagStrings.HelpShort,
                },
                [CommandStrings.CD] = new List<string>()
                {
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CDiCorrectOffset,
                    FlagStrings.CDiReadyNormalize,
                    FlagStrings.DescrambleNew,
                    FlagStrings.Drive,
                    FlagStrings.ForceOffset,
                    FlagStrings.ForceQTOC,
                    FlagStrings.ForceSplit,
                    FlagStrings.ForceTOC,
                    FlagStrings.ISO9660Trim,
                    FlagStrings.ImageName,
                    FlagStrings.ImagePath,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.Overwrite,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Retries,
                    FlagStrings.RingSize,
                    FlagStrings.Skip,
                    FlagStrings.SkipFill,
                    FlagStrings.SkipLeadIn,
                    FlagStrings.SkipSize,
                    FlagStrings.Speed,
                    FlagStrings.StopLBA,
                    FlagStrings.Unsupported,
                    FlagStrings.Verbose,
                },
                [CommandStrings.Dump] = new List<string>()
                {
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CDiCorrectOffset,
                    FlagStrings.CDiReadyNormalize,
                    FlagStrings.DescrambleNew,
                    FlagStrings.Drive,
                    FlagStrings.ForceOffset,
                    FlagStrings.ForceQTOC,
                    FlagStrings.ForceSplit,
                    FlagStrings.ForceTOC,
                    FlagStrings.ISO9660Trim,
                    FlagStrings.ImageName,
                    FlagStrings.ImagePath,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.Overwrite,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Retries,
                    FlagStrings.RingSize,
                    FlagStrings.Skip,
                    FlagStrings.SkipFill,
                    FlagStrings.SkipLeadIn,
                    FlagStrings.SkipSize,
                    FlagStrings.Speed,
                    FlagStrings.StopLBA,
                    FlagStrings.Unsupported,
                    FlagStrings.Verbose,
                },
                [CommandStrings.Info] = new List<string>()
                {
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CDiCorrectOffset,
                    FlagStrings.CDiReadyNormalize,
                    FlagStrings.DescrambleNew,
                    FlagStrings.Drive,
                    FlagStrings.ForceOffset,
                    FlagStrings.ForceQTOC,
                    FlagStrings.ForceSplit,
                    FlagStrings.ForceTOC,
                    FlagStrings.ISO9660Trim,
                    FlagStrings.ImageName,
                    FlagStrings.ImagePath,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.Overwrite,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Retries,
                    FlagStrings.RingSize,
                    FlagStrings.Skip,
                    FlagStrings.SkipFill,
                    FlagStrings.SkipLeadIn,
                    FlagStrings.SkipSize,
                    FlagStrings.Speed,
                    FlagStrings.StopLBA,
                    FlagStrings.Unsupported,
                    FlagStrings.Verbose,
                },
                [CommandStrings.Protection] = new List<string>()
                {
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CDiCorrectOffset,
                    FlagStrings.CDiReadyNormalize,
                    FlagStrings.DescrambleNew,
                    FlagStrings.Drive,
                    FlagStrings.ForceOffset,
                    FlagStrings.ForceQTOC,
                    FlagStrings.ForceSplit,
                    FlagStrings.ForceTOC,
                    FlagStrings.ISO9660Trim,
                    FlagStrings.ImageName,
                    FlagStrings.ImagePath,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.Overwrite,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Retries,
                    FlagStrings.RingSize,
                    FlagStrings.Skip,
                    FlagStrings.SkipFill,
                    FlagStrings.SkipLeadIn,
                    FlagStrings.SkipSize,
                    FlagStrings.Speed,
                    FlagStrings.StopLBA,
                    FlagStrings.Unsupported,
                    FlagStrings.Verbose,
                },
                [CommandStrings.Refine] = new List<string>()
                {
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CDiCorrectOffset,
                    FlagStrings.CDiReadyNormalize,
                    FlagStrings.DescrambleNew,
                    FlagStrings.Drive,
                    FlagStrings.ForceOffset,
                    FlagStrings.ForceQTOC,
                    FlagStrings.ForceSplit,
                    FlagStrings.ForceTOC,
                    FlagStrings.ISO9660Trim,
                    FlagStrings.ImageName,
                    FlagStrings.ImagePath,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.Overwrite,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Retries,
                    FlagStrings.RingSize,
                    FlagStrings.Skip,
                    FlagStrings.SkipFill,
                    FlagStrings.SkipLeadIn,
                    FlagStrings.SkipSize,
                    FlagStrings.Speed,
                    FlagStrings.StopLBA,
                    FlagStrings.Unsupported,
                    FlagStrings.Verbose,
                },
                [CommandStrings.Split] = new List<string>()
                {
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CDiCorrectOffset,
                    FlagStrings.CDiReadyNormalize,
                    FlagStrings.DescrambleNew,
                    FlagStrings.Drive,
                    FlagStrings.ForceOffset,
                    FlagStrings.ForceQTOC,
                    FlagStrings.ForceSplit,
                    FlagStrings.ForceTOC,
                    FlagStrings.ISO9660Trim,
                    FlagStrings.ImageName,
                    FlagStrings.ImagePath,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.Overwrite,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Retries,
                    FlagStrings.RingSize,
                    FlagStrings.Skip,
                    FlagStrings.SkipFill,
                    FlagStrings.SkipLeadIn,
                    FlagStrings.SkipSize,
                    FlagStrings.Speed,
                    FlagStrings.StopLBA,
                    FlagStrings.Unsupported,
                    FlagStrings.Verbose,
                },
            };
        }

        /// <inheritdoc/>
        public override string GetDefaultExtension(MediaType? mediaType) => ".bin"; // TODO: Fill out

        /// <inheritdoc/>
        public override bool IsDumpingCommand()
        {
            switch (this.BaseCommand)
            {
                case CommandStrings.CD:
                case CommandStrings.Dump:
                    return true;
                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        protected override void ResetValues()
        {
            BaseCommand = CommandStrings.NONE;

            flags = new Dictionary<string, bool?>();

            AudioSilenceThresholdValue = null;
            DriveValue = null;
            ForceOffsetValue = null;
            ImageNameValue = null;
            ImagePathValue = null;
            RetriesValue = null;
            RingSizeValue = null;
            SkipValue = null;
            SkipFillValue = null;
            SkipSizeValue = null;
            SpeedValue = null;
            StopLBAValue = null;
        }

        /// <inheritdoc/>
        protected override void SetDefaultParameters(char driveLetter, string filename, int? driveSpeed, Options options)
        {
            // TODO: Fill out
        }

        /// <inheritdoc/>
        protected override bool ValidateAndSetParameters(string parameters)
        {
            BaseCommand = CommandStrings.NONE;

            // The string has to be valid by itself first
            if (string.IsNullOrWhiteSpace(parameters))
                return false;

            // Now split the string into parts for easier validation
            // https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
            parameters = parameters.Trim();
            List<string> parts = Regex.Matches(parameters, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();

            // Determine what the commandline should look like given the first item
            BaseCommand = parts[0];

            int index = 0;
            switch (BaseCommand)
            {
                // These two are technically CommandStrings.NONE
                case FlagStrings.HelpLong:
                case FlagStrings.HelpShort:

                // Normal commands
                case CommandStrings.CD:
                case CommandStrings.Dump:
                case CommandStrings.Info:
                case CommandStrings.Protection:
                case CommandStrings.Refine:
                case CommandStrings.Split:
                    break;

                default:
                    return false;
            }

            // Loop through all auxiliary flags, if necessary
            for (int i = index; i < parts.Count; i++)
            {
                // Flag read-out values
                byte? byteValue = null;
                int? intValue = null;
                string stringValue = null;

                // Audio Silence Threshold
                intValue = ProcessInt32Parameter(parts, FlagStrings.AudioSilenceThreshold, ref i);
                if (intValue != null)
                {
                    if (intValue >= 0)
                        AudioSilenceThresholdValue = intValue;
                    else
                        return false;
                }

                // CD-i Correct Offset
                ProcessFlagParameter(parts, FlagStrings.CDiCorrectOffset, ref i);

                // CD-i Ready Normalize
                ProcessFlagParameter(parts, FlagStrings.CDiReadyNormalize, ref i);

                // Descramble New
                ProcessFlagParameter(parts, FlagStrings.DescrambleNew, ref i);

                // Drive -- TODO: No drive is technically supported
                stringValue = ProcessStringParameter(parts, FlagStrings.Drive, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    DriveValue = stringValue;

                // Force Offset
                intValue = ProcessInt32Parameter(parts, FlagStrings.ForceOffset, ref i);
                if (intValue != null)
                    ForceOffsetValue = intValue;

                // Force QTOC
                ProcessFlagParameter(parts, FlagStrings.ForceQTOC, ref i);

                // Force Split
                ProcessFlagParameter(parts, FlagStrings.ForceSplit, ref i);

                // Force TOC
                ProcessFlagParameter(parts, FlagStrings.ForceTOC, ref i);

                // Help
                ProcessFlagParameter(parts, FlagStrings.HelpShort, FlagStrings.HelpLong, ref i);

                // ISO9660 Trim
                ProcessFlagParameter(parts, FlagStrings.ISO9660Trim, ref i);

                // Image Name -- TODO: Empty image name technically supported
                stringValue = ProcessStringParameter(parts, FlagStrings.ImageName, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    ImageNameValue = stringValue;

                // Image Path -- TODO: Empty image path technically supported
                stringValue = ProcessStringParameter(parts, FlagStrings.ImagePath, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    ImagePathValue = stringValue;

                // Leave Unchanged
                ProcessFlagParameter(parts, FlagStrings.LeaveUnchanged, ref i);

                // Overwrite
                ProcessFlagParameter(parts, FlagStrings.Overwrite, ref i);

                // Refine Subchannel
                ProcessFlagParameter(parts, FlagStrings.RefineSubchannel, ref i);

                // Retries
                intValue = ProcessInt32Parameter(parts, FlagStrings.Retries, ref i);
                if (intValue != null)
                {
                    if (intValue >= 0)
                        RetriesValue = intValue;
                    else
                        return false;
                }

                // Ring Size
                intValue = ProcessInt32Parameter(parts, FlagStrings.RingSize, ref i);
                if (intValue != null)
                {
                    if (intValue >= 0)
                        RetriesValue = intValue;
                    else
                        return false;
                }

                // Skip -- TODO: Validate how this value should look
                stringValue = ProcessStringParameter(parts, FlagStrings.Skip, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    SkipValue = stringValue;

                // Skip Fill
                byteValue = ProcessUInt8Parameter(parts, FlagStrings.RingSize, ref i);
                if (byteValue != null)
                    SkipFillValue = byteValue;

                // Skip Lead-In
                ProcessFlagParameter(parts, FlagStrings.SkipLeadIn, ref i);

                // Skip Size
                intValue = ProcessInt32Parameter(parts, FlagStrings.SkipSize, ref i);
                if (intValue != null)
                {
                    if (intValue >= 0)
                        SkipSizeValue = intValue;
                    else
                        return false;
                }

                // Speed
                intValue = ProcessInt32Parameter(parts, FlagStrings.Speed, ref i);
                if (intValue != null)
                {
                    if (intValue >= 1)
                        Speed = intValue;
                    else
                        return false;
                }

                // Stop LBA
                intValue = ProcessInt32Parameter(parts, FlagStrings.StopLBA, ref i);
                if (intValue != null)
                    StopLBAValue = intValue;

                // Unsupported
                ProcessFlagParameter(parts, FlagStrings.Unsupported, ref i);

                // Verbose
                ProcessFlagParameter(parts, FlagStrings.Verbose, ref i);
            }

            return true;
        }

        #endregion
    }
}
