using UnityEditor;

/// <summary>
/// Importación de audio pensada para no trabar el juego:
/// - Música (carpeta "music"): Streaming + carga en segundo plano + Vorbis 0,6. Antes era
///   "Decompress On Load" sin precarga: al empezar cada canción se descomprimía el MP3 entero en
///   el hilo principal (~30 MB de PCM) y el juego se congelaba un instante.
/// - Efectos: Decompress On Load con precarga, para que el primer uso de cada sonido no trabe.
/// </summary>
public class AudioImportRules : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        bool music = assetPath.Replace('\\', '/').Contains("/music/");

        if (music)
        {
            settings.loadType = UnityEngine.AudioClipLoadType.Streaming;
            settings.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            settings.preloadAudioData = false;
            importer.loadInBackground = true;
        }
        else
        {
            settings.loadType = UnityEngine.AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.loadInBackground = false;
        }
        importer.defaultSampleSettings = settings;
    }
}
