// Runtime map loader for LDtk levels (via LDtkToUnity).
//
// Pipeline: build the map in LDtk -> drop the .ldtk file into Assets/Maps/ ->
// LDtkToUnity imports it as a prefab. Assign that prefab to 'ldtkProjectPrefab'
// here. The imported hierarchy already contains Unity Grid + Tilemap components
// per layer; this loader instantiates it and switches the active level.
using System.Collections.Generic;
using System.Linq;
using LDtkUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

namespace Mygame.Map
{
    public class MapLoader : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("The prefab LDtkToUnity generated from your .ldtk file (in Assets/Maps).")]
        [SerializeField] private GameObject ldtkProjectPrefab;

        [Header("Startup")]
        [SerializeField] private bool instantiateOnAwake = true;
        [Tooltip("Level Identifier to show first. Empty = show all / leave as imported.")]
        [SerializeField] private string startLevelIdentifier = "";
        [Tooltip("Hide every level except the active one when a level is loaded.")]
        [SerializeField] private bool singleActiveLevel = true;

        [Header("Events")]
        public UnityEvent<string> OnLevelLoaded;

        public LDtkComponentProject Project { get; private set; }
        public LDtkComponentLevel ActiveLevel { get; private set; }

        private GameObject _root;
        private readonly Dictionary<string, LDtkComponentLevel> _levels = new();

        private void Awake()
        {
            if (instantiateOnAwake) Build();
        }

        /// <summary>Instantiate the LDtk project and index its levels.</summary>
        public void Build()
        {
            if (_root != null) return;

            if (ldtkProjectPrefab != null)
                _root = Instantiate(ldtkProjectPrefab, transform);
            else
                _root = gameObject; // assume the project is already in this hierarchy

            Project = _root.GetComponentInChildren<LDtkComponentProject>(true);
            if (Project == null)
            {
                Debug.LogError("[Map] No LDtkComponentProject found. Assign the imported .ldtk prefab.");
                return;
            }

            _levels.Clear();
            foreach (var level in _root.GetComponentsInChildren<LDtkComponentLevel>(true))
                _levels[level.Identifier] = level;

            if (!string.IsNullOrEmpty(startLevelIdentifier))
                LoadLevel(startLevelIdentifier);
        }

        /// <summary>Activate a level by its LDtk Identifier.</summary>
        public bool LoadLevel(string identifier)
        {
            if (_levels.Count == 0) Build();

            if (!_levels.TryGetValue(identifier, out var level))
            {
                Debug.LogError($"[Map] Level '{identifier}' not found. " +
                               $"Available: {string.Join(", ", _levels.Keys)}");
                return false;
            }

            if (singleActiveLevel)
                foreach (var kv in _levels)
                    kv.Value.gameObject.SetActive(kv.Value == level);
            else
                level.gameObject.SetActive(true);

            ActiveLevel = level;
            OnLevelLoaded?.Invoke(identifier);
            return true;
        }

        public IEnumerable<string> LevelIdentifiers => _levels.Keys;

        // ---- Tilemap / layer access on the active level ----

        /// <summary>All Tilemaps in the active level (one per tile/auto layer).</summary>
        public Tilemap[] GetTilemaps() =>
            ActiveLevel == null
                ? System.Array.Empty<Tilemap>()
                : ActiveLevel.GetComponentsInChildren<Tilemap>(true);

        /// <summary>Tilemap for a specific LDtk layer Identifier, or null.</summary>
        public Tilemap GetTilemap(string layerIdentifier)
        {
            var layer = GetLayer(layerIdentifier);
            return layer != null ? layer.GetComponent<Tilemap>() : null;
        }

        /// <summary>The LDtk layer component by Identifier on the active level.</summary>
        public LDtkComponentLayer GetLayer(string layerIdentifier)
        {
            if (ActiveLevel == null) return null;
            return ActiveLevel.LayerInstances?
                .FirstOrDefault(l => l != null && l.Identifier == layerIdentifier);
        }

        /// <summary>Entity instances on a given layer of the active level.</summary>
        public LDtkComponentEntity[] GetEntities(string layerIdentifier)
        {
            var layer = GetLayer(layerIdentifier);
            return layer != null ? layer.EntityInstances : System.Array.Empty<LDtkComponentEntity>();
        }

        /// <summary>The Grid component of the active level (for cell/world conversions).</summary>
        public Grid GetGrid() =>
            ActiveLevel != null ? ActiveLevel.GetComponentInChildren<Grid>(true) : null;
    }
}
