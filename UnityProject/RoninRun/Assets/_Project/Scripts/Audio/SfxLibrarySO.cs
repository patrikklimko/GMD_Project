using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that maps SfxId values to AudioClip assets.
/// Lets gameplay code call AudioManager.Instance.PlaySfx(SfxId.Foo)
/// without holding direct AudioClip references everywhere.
///
/// Backed by a serialized list (rather than a Dictionary) so it is
/// inspector-editable and version-control-friendly. The lookup is
/// built once on first access and cached.
/// </summary>
[CreateAssetMenu(
    fileName = "SfxLibrary",
    menuName = "RoninRun/Sfx Library",
    order = 1)]
public class SfxLibrarySO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float defaultVolume = 1f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<SfxId, Entry> _index;

    public AudioClip GetClip(SfxId id)
    {
        EnsureIndex();
        return _index.TryGetValue(id, out Entry e) ? e.clip : null;
    }

    public float GetDefaultVolume(SfxId id)
    {
        EnsureIndex();
        return _index.TryGetValue(id, out Entry e) ? e.defaultVolume : 1f;
    }

    private void EnsureIndex()
    {
        if (_index != null) return;
        _index = new Dictionary<SfxId, Entry>(entries.Count);
        foreach (Entry e in entries)
        {
            if (e == null) continue;
            _index[e.id] = e;
        }
    }

    private void OnValidate()
    {
        _index = null; // rebuild lazily in editor when inspector changes.
    }
}
