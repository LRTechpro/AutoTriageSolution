using System;
using AutoTriage.Core.Decoding;

namespace AutoTriage.Core.Decoding
{
    /// <summary>
    /// Example configuration for OEM-specific dictionaries.
    /// Copy this pattern to add your own automotive manufacturer's codes.
    /// </summary>
    public static class OemDictionaryExample
    {
        /// <summary>
        /// Call this at application startup to load OEM-specific dictionaries
        /// </summary>
        public static void ConfigureOemDictionaries()
        {
            // ═══════════════════════════════════════════════════════════════
            // EXAMPLE: Generic OEM DIDs
            // ═══════════════════════════════════════════════════════════════

            // Standard ODX DIDs (common across many OEMs)
            AutomotivePayloadDecoder.DidDictionary[0xF186] = "ActiveDiagnosticSessionDataIdentifier";
            AutomotivePayloadDecoder.DidDictionary[0xF187] = "VehicleManufacturerSparePartNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF188] = "VehicleManufacturerECUSoftwareVersionNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF189] = "VehicleManufacturerECUSoftwareNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF18A] = "VehicleManufacturerECUSoftwareNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF18B] = "ECUManufacturingDateAndTime";
            AutomotivePayloadDecoder.DidDictionary[0xF18C] = "ECUSerialNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF18E] = "SystemSupplierECUSoftwareNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF190] = "VIN";
            AutomotivePayloadDecoder.DidDictionary[0xF191] = "VehicleManufacturerECUHardwareNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF192] = "SystemSupplierECUHardwareNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF193] = "SystemSupplierECUHardwareVersionNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF194] = "SystemSupplierECUSoftwareVersionNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF195] = "SoftwareModuleIdentifier";
            AutomotivePayloadDecoder.DidDictionary[0xF197] = "SystemNameOrEngineType";
            AutomotivePayloadDecoder.DidDictionary[0xF198] = "RepairShopCodeOrTesterSerialNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF199] = "ProgrammingDate";
            AutomotivePayloadDecoder.DidDictionary[0xF19D] = "ECUInstallationDate";
            AutomotivePayloadDecoder.DidDictionary[0xF19E] = "VehicleManufacturerKitAssemblyPartNumber";

            // ═══════════════════════════════════════════════════════════════
            // EXAMPLE: Ford-specific DIDs (example values)
            // ═══════════════════════════════════════════════════════════════
            /*
            AutomotivePayloadDecoder.DidDictionary[0xDE00] = "Ford_CalibrationVerificationNumber";
            AutomotivePayloadDecoder.DidDictionary[0xDE01] = "Ford_ECUManufacturingDate";
            AutomotivePayloadDecoder.DidDictionary[0xDE02] = "Ford_ECUSerialNumber";
            AutomotivePayloadDecoder.DidDictionary[0xDE03] = "Ford_ModulePartNumber";
            */

            // ═══════════════════════════════════════════════════════════════
            // EXAMPLE: GM-specific DIDs (example values)
            // ═══════════════════════════════════════════════════════════════
            /*
            AutomotivePayloadDecoder.DidDictionary[0x0100] = "GM_VIN";
            AutomotivePayloadDecoder.DidDictionary[0x0101] = "GM_CalibrationID";
            AutomotivePayloadDecoder.DidDictionary[0x0102] = "GM_SoftwarePartNumber";
            */

            // ═══════════════════════════════════════════════════════════════
            // EXAMPLE: Toyota-specific DIDs (example values)
            // ═══════════════════════════════════════════════════════════════
            /*
            AutomotivePayloadDecoder.DidDictionary[0xF100] = "Toyota_ECUPartNumber";
            AutomotivePayloadDecoder.DidDictionary[0xF101] = "Toyota_ApplicationSoftwareID";
            AutomotivePayloadDecoder.DidDictionary[0xF102] = "Toyota_DatabaseID";
            */

            // ═══════════════════════════════════════════════════════════════
            // EXAMPLE: Common Routine IDs
            // ═══════════════════════════════════════════════════════════════

            // Standard routines (common)
            AutomotivePayloadDecoder.RoutineIdDictionary[0x0202] = "ProgrammingPreconditionCheck";
            AutomotivePayloadDecoder.RoutineIdDictionary[0x0203] = "CheckMemory";
            AutomotivePayloadDecoder.RoutineIdDictionary[0xFF00] = "EraseMemory";
            AutomotivePayloadDecoder.RoutineIdDictionary[0xFF01] = "CheckProgrammingDependencies";
            AutomotivePayloadDecoder.RoutineIdDictionary[0xF018] = "ReadDevelopmentData";

            // ═══════════════════════════════════════════════════════════════
            // EXAMPLE: OEM-specific Routine IDs
            // ═══════════════════════════════════════════════════════════════
            /*
            AutomotivePayloadDecoder.RoutineIdDictionary[0x0301] = "OEM_CheckVehicleConfiguration";
            AutomotivePayloadDecoder.RoutineIdDictionary[0x0401] = "OEM_WriteVIN";
            AutomotivePayloadDecoder.RoutineIdDictionary[0x0501] = "OEM_ResetAdaptiveValues";
            */
        }

        /// <summary>
        /// Example: Load dictionaries from a configuration file or database
        /// </summary>
        public static void LoadFromConfigFile(string filePath)
        {
            // Example implementation:
            // 1. Read JSON/XML/CSV file with DID mappings
            // 2. Parse each line: "0xF190,VIN"
            // 3. Add to dictionary

            /*
            var lines = System.IO.File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var didHex = parts[0].Trim();
                    var name = parts[1].Trim();

                    if (didHex.StartsWith("0x"))
                        didHex = didHex.Substring(2);

                    if (ushort.TryParse(didHex, System.Globalization.NumberStyles.HexNumber, null, out ushort did))
                    {
                        AutomotivePayloadDecoder.DidDictionary[did] = name;
                    }
                }
            }
            */

            throw new NotImplementedException("Implement based on your config file format");
        }
    }
}
