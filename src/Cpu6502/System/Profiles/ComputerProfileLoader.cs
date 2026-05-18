using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cpu6502.System.Profiles;

/// <summary>
/// Options for loading a computer profile.
/// </summary>
/// <param name="RomDataOverrides">Optional dictionary mapping ROM file paths to byte arrays for testing.</param>
/// <param name="BasePath">Optional base path for resolving relative file paths.</param>
/// <param name="TerminalLinks">Optional dictionary mapping device ids to terminal links.</param>
public sealed record ProfileLoadOptions(
    Dictionary<string, byte[]>? RomDataOverrides = null,
    string? BasePath = null,
    Dictionary<string, Terminal.ITerminalLink>? TerminalLinks = null);

/// <summary>
/// Exception thrown when a computer profile fails validation.
/// </summary>
public sealed class ComputerProfileValidationException : Exception
{
    /// <summary>
    /// Creates a new validation exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="profileId">The profile identifier.</param>
    public ComputerProfileValidationException(string message, string? profileId = null)
        : base(profileId != null ? $"{message} Profile: {profileId}" : message)
    {
        ProfileId = profileId ?? string.Empty;
    }

    /// <summary>The profile identifier associated with the error.</summary>
    public string ProfileId { get; }
}

/// <summary>
/// Loader for computer profiles from JSON files.
/// </summary>
public sealed class ComputerProfileLoader
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Creates a new ComputerProfileLoader with default options.
    /// </summary>
    public ComputerProfileLoader()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters =
            {
                new AddressSpaceKindConverter(),
                new JsonObjectConverter()
            }
        };
    }

    /// <summary>
    /// Loads a computer profile from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the profile.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The loaded computer profile.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    /// <exception cref="ComputerProfileValidationException">Thrown when the profile fails validation.</exception>
    public ComputerProfile LoadFromString(string json, ProfileLoadOptions? loadOptions = null)
    {
        try
        {
            var profile = DeserializeProfile(json);
            profile = ReplaceRomFilesWithData(profile, loadOptions);
            profile.Validate();
            return profile;
        }
        catch (JsonException ex)
        {
            throw new ComputerProfileValidationException(ex.Message);
        }
        catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is FormatException)
        {
            throw new ComputerProfileValidationException(ex.Message);
        }
    }

    /// <summary>
    /// Loads a computer profile from a file.
    /// </summary>
    /// <param name="filePath">Path to the JSON profile file.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The loaded computer profile.</returns>
    public ComputerProfile LoadFromFile(string filePath, ProfileLoadOptions? loadOptions = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Profile file not found: {filePath}");

        var json = File.ReadAllText(filePath);

        // Set base path from file directory if not specified
        loadOptions ??= new ProfileLoadOptions();
        loadOptions = loadOptions with { BasePath = Path.GetDirectoryName(filePath) };

        return LoadFromString(json, loadOptions);
    }

    private ComputerProfile DeserializeProfile(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Parse basic fields
        var schema = root.GetProperty("schema").GetString() ?? throw new JsonException("Missing 'schema' field.");
        var id = root.GetProperty("id").GetString() ?? throw new JsonException("Missing 'id' field.");
        var name = root.GetProperty("name").GetString() ?? id;
        var status = root.GetProperty("status").GetString() ?? "planned";

        // Parse CPU
        var cpuElement = root.GetProperty("cpu");
        var cpu = ParseCpuProfile(cpuElement, id);

        // Parse address space
        var addressSpaceElement = root.GetProperty("addressSpace");
        var addressSpace = ParseAddressSpaceProfile(addressSpaceElement, id);

        // Parse memory
        var memoryElement = root.GetProperty("memory");
        var memory = ParseMemoryProfile(memoryElement, id);

        // Parse devices
        var devices = ParseDevices(root.GetProperty("devices"), id, addressSpace);

        return new ComputerProfile(
            schema,
            id,
            name,
            status,
            cpu,
            addressSpace,
            memory,
            devices);
    }

    private CpuProfile ParseCpuProfile(JsonElement element, string profileId)
    {
        var type = element.GetProperty("type").GetString() ?? throw new JsonException("CPU 'type' is required.");
        var clockHz = element.GetProperty("clockHz").GetInt64();
        var initialPC = element.TryGetProperty("initialPC", out var pcElement) ? (uint?)pcElement.GetUInt32() : null;

        return new CpuProfile(type, clockHz, initialPC);
    }

    private AddressSpaceProfile ParseAddressSpaceProfile(JsonElement element, string profileId)
    {
        var memoryBits = element.GetProperty("memoryAddressBits").GetInt32();
        var portBits = element.GetProperty("portAddressBits").GetInt32();
        var hasSeparatePorts = element.GetProperty("hasSeparatePortSpace").GetBoolean();
        var dataBusBits = element.GetProperty("dataBusBits").GetInt32();

        return new AddressSpaceProfile(memoryBits, portBits, hasSeparatePorts, dataBusBits);
    }

    private MemoryProfile ParseMemoryProfile(JsonElement element, string profileId)
    {
        var ramList = new List<RamRegionProfile>();
        var romList = new List<RomRegionProfile>();

        if (element.TryGetProperty("ram", out var ramArray))
        {
            foreach (var ramItem in ramArray.EnumerateArray())
            {
                var ram = ParseRamRegion(ramItem);
                ramList.Add(ram);
            }
        }

        if (element.TryGetProperty("rom", out var romArray))
        {
            foreach (var romItem in romArray.EnumerateArray())
            {
                var rom = ParseRomRegion(romItem);
                romList.Add(rom);
            }
        }

        return new MemoryProfile(ramList, romList);
    }

    private RamRegionProfile ParseRamRegion(JsonElement element)
    {
        var id = element.GetProperty("id").GetString() ?? throw new JsonException("RAM region 'id' is required.");
        var start = element.GetProperty("start").GetString() ?? throw new JsonException("RAM region 'start' is required.");
        var size = element.GetProperty("size").GetString() ?? throw new JsonException("RAM region 'size' is required.");
        var fillValue = element.TryGetProperty("fillValue", out var fillElement) ? fillElement.GetByte() : (byte)0;

        return new RamRegionProfile(id, start, size, fillValue);
    }

    private RomRegionProfile ParseRomRegion(JsonElement element)
    {
        var id = element.GetProperty("id").GetString() ?? throw new JsonException("ROM region 'id' is required.");
        var start = element.GetProperty("start").GetString() ?? throw new JsonException("ROM region 'start' is required.");
        var size = element.GetProperty("size").GetString() ?? throw new JsonException("ROM region 'size' is required.");
        var file = element.TryGetProperty("file", out var fileElement) ? fileElement.GetString() : null;

        var writePolicy = RomWritePolicy.ThrowException;
        if (element.TryGetProperty("writePolicy", out var policyElement))
        {
            var policyString = policyElement.GetString() ?? "ThrowException";
            Enum.TryParse(policyString, true, out writePolicy);
        }

        return new RomRegionProfile(id, start, size, file, writePolicy);
    }

    private ImmutableArray<DeviceProfile> ParseDevices(JsonElement element, string profileId, AddressSpaceProfile addressSpace)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return ImmutableArray<DeviceProfile>.Empty;

        var devices = new List<DeviceProfile>();
        foreach (var deviceElement in element.EnumerateArray())
        {
            var device = ParseDevice(deviceElement, profileId);
            device.Validate(profileId, addressSpace);
            devices.Add(device);
        }

        return devices.ToImmutableArray();
    }

    private DeviceProfile ParseDevice(JsonElement element, string profileId)
    {
        var id = element.GetProperty("id").GetString() ?? throw new JsonException("Device 'id' is required.");
        var type = element.GetProperty("type").GetString() ?? throw new JsonException("Device 'type' is required.");

        // Parse mapping
        var mappingElement = element.GetProperty("mapping");
        var kind = mappingElement.GetProperty("kind").GetString() ?? throw new JsonException("Device mapping 'kind' is required.");
        
        if (!Enum.TryParse<AddressSpaceKind>(kind, true, out var mappingKind))
            throw new JsonException($"Invalid mapping kind: '{kind}'. Expected 'memory' or 'port'.");

        var baseAddress = mappingElement.GetProperty("baseAddress").GetString() ?? throw new JsonException("Device mapping 'baseAddress' is required.");
        var size = mappingElement.GetProperty("size").GetString() ?? throw new JsonException("Device mapping 'size' is required.");

        var mapping = new DeviceMappingProfile(mappingKind, baseAddress, size);

        // Parse optional bindings and options
        JsonObject? bindings = null;
        JsonObject? options = null;

        if (element.TryGetProperty("bindings", out var bindingsElement))
        {
            bindings = bindingsElement.Deserialize<JsonObject>() ?? new JsonObject();
        }

        if (element.TryGetProperty("options", out var optionsElement))
        {
            options = optionsElement.Deserialize<JsonObject>() ?? new JsonObject();
        }

        if (element.TryGetProperty("preset", out var presetElement))
        {
            options ??= new JsonObject();
            options["preset"] = presetElement.GetString();
        }

        return new DeviceProfile(id, type, mapping, bindings, options);
    }

    private ComputerProfile ReplaceRomFilesWithData(ComputerProfile profile, ProfileLoadOptions? loadOptions)
    {
        if (loadOptions?.RomDataOverrides == null || profile.Memory.RomRegions.IsDefaultOrEmpty)
            return profile;

        var newRomRegions = new List<RomRegionProfile>();
        foreach (var rom in profile.Memory.RomRegions)
        {
            if (rom.File != null && loadOptions.RomDataOverrides.TryGetValue(rom.File, out var data))
            {
                // Create a new ROM region without the file reference
                newRomRegions.Add(rom with { File = null });
            }
            else
            {
                newRomRegions.Add(rom);
            }
        }

        var newMemory = profile.Memory with { RomRegions = newRomRegions.ToImmutableArray() };
        return profile with { Memory = newMemory };
    }

    // JSON converter for AddressSpaceKind
    private sealed class AddressSpaceKindConverter : JsonConverter<AddressSpaceKind>
    {
        public override AddressSpaceKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null)
                return AddressSpaceKind.Memory;

            if (Enum.TryParse(value, true, out AddressSpaceKind result))
                return result;

            throw new JsonException($"Invalid AddressSpaceKind value: '{value}'. Expected 'memory' or 'port'.");
        }

        public override void Write(Utf8JsonWriter writer, AddressSpaceKind value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToLowerInvariant());
        }
    }

    // JSON converter for JsonObject
    private sealed class JsonObjectConverter : JsonConverter<JsonObject>
    {
        public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return JsonNode.Parse(doc.RootElement.GetRawText())?.AsObject() ?? new JsonObject();
            }
            return new JsonObject();
        }

        public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
        {
            value.WriteTo(writer, options);
        }
    }
}
