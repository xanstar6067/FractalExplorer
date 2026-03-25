using System.Text.Json;

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    /// <summary>
    /// Метаданные цветового профиля, связанного с конкретной палитрой.
    /// </summary>
    public sealed class ColorMetadataProfile
    {
        public Guid ProfileId { get; set; } = Guid.NewGuid();
        public Guid PaletteId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// CRUD-менеджер sidecar-файла метапрофилей цветовых палитр.
    /// </summary>
    public sealed class ColorMetadataProfileManager
    {
        private const string MetadataProfilesFileName = "color_metadata_profiles.json";
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public ColorMetadataProfileManager()
        {
            string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
            Directory.CreateDirectory(savesDirectory);
            _filePath = Path.Combine(savesDirectory, MetadataProfilesFileName);
        }

        public List<ColorMetadataProfile> GetAllProfiles() => LoadProfiles();

        public List<ColorMetadataProfile> GetProfilesForPalette(Guid paletteId)
        {
            if (paletteId == Guid.Empty)
            {
                return new List<ColorMetadataProfile>();
            }

            return LoadProfiles()
                .Where(p => p.PaletteId == paletteId)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public ColorMetadataProfile CreateProfile(Guid paletteId, string? name = null)
        {
            if (paletteId == Guid.Empty)
            {
                throw new ArgumentException("PaletteId не может быть пустым.", nameof(paletteId));
            }

            List<ColorMetadataProfile> profiles = LoadProfiles();
            string effectiveName = BuildUniqueProfileName(profiles, paletteId, name);

            var profile = new ColorMetadataProfile
            {
                ProfileId = Guid.NewGuid(),
                PaletteId = paletteId,
                Name = effectiveName,
                CreatedUtc = DateTime.UtcNow
            };

            profiles.Add(profile);
            SaveProfiles(profiles);
            return profile;
        }

        public bool UpdateProfile(ColorMetadataProfile updatedProfile)
        {
            if (updatedProfile == null || updatedProfile.ProfileId == Guid.Empty || updatedProfile.PaletteId == Guid.Empty)
            {
                return false;
            }

            List<ColorMetadataProfile> profiles = LoadProfiles();
            int index = profiles.FindIndex(p => p.ProfileId == updatedProfile.ProfileId);
            if (index < 0)
            {
                return false;
            }

            profiles[index] = updatedProfile;
            SaveProfiles(profiles);
            return true;
        }

        public bool DeleteProfile(Guid profileId)
        {
            if (profileId == Guid.Empty)
            {
                return false;
            }

            List<ColorMetadataProfile> profiles = LoadProfiles();
            int removed = profiles.RemoveAll(p => p.ProfileId == profileId);
            if (removed == 0)
            {
                return false;
            }

            SaveProfiles(profiles);
            return true;
        }

        public int DeleteProfilesForPalette(Guid paletteId)
        {
            if (paletteId == Guid.Empty)
            {
                return 0;
            }

            List<ColorMetadataProfile> profiles = LoadProfiles();
            int removed = profiles.RemoveAll(p => p.PaletteId == paletteId);
            if (removed > 0)
            {
                SaveProfiles(profiles);
            }

            return removed;
        }

        public int PruneOrphanProfiles(IEnumerable<Guid> existingPaletteIds)
        {
            HashSet<Guid> paletteIdSet = new(existingPaletteIds.Where(id => id != Guid.Empty));
            List<ColorMetadataProfile> profiles = LoadProfiles();
            int removed = profiles.RemoveAll(p => p.PaletteId == Guid.Empty || !paletteIdSet.Contains(p.PaletteId));

            if (removed > 0)
            {
                SaveProfiles(profiles);
            }

            return removed;
        }

        public void ReplaceAllProfiles(IEnumerable<ColorMetadataProfile> profiles)
        {
            SaveProfiles(profiles.ToList());
        }

        private List<ColorMetadataProfile> LoadProfiles()
        {
            if (!File.Exists(_filePath))
            {
                return new List<ColorMetadataProfile>();
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<ColorMetadataProfile>>(json, _jsonOptions) ?? new List<ColorMetadataProfile>();
            }
            catch
            {
                return new List<ColorMetadataProfile>();
            }
        }

        private void SaveProfiles(List<ColorMetadataProfile> profiles)
        {
            string json = JsonSerializer.Serialize(profiles, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }

        private static string BuildUniqueProfileName(IReadOnlyCollection<ColorMetadataProfile> allProfiles, Guid paletteId, string? requestedName)
        {
            string baseName = string.IsNullOrWhiteSpace(requestedName) ? "Метапрофиль" : requestedName.Trim();
            HashSet<string> names = allProfiles
                .Where(p => p.PaletteId == paletteId)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!names.Contains(baseName))
            {
                return baseName;
            }

            int counter = 2;
            string candidate;
            do
            {
                candidate = $"{baseName} {counter++}";
            }
            while (names.Contains(candidate));

            return candidate;
        }
    }
}
