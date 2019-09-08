namespace HandlingEditor.Client
{
    public interface IPresetManager
    {
        /// <summary>
        /// Saves the <paramref name="preset"/> using the <paramref name="name"/> as preset name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="preset"></param>
        /// <returns></returns>
        bool Save(string name, HandlingPreset preset);

        /// <summary>
        /// Deletes the preset with the <paramref name="name"/> as preset name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Delete(string name);

        /// <summary>
        /// Loads and the returns the <see cref="HandlingPreset"/> named <paramref name="name"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        HandlingPreset Load(string name);
    }
}
