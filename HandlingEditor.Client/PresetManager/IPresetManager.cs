using System;
using System.Collections.Generic;

namespace HandlingEditor.Client
{
    public interface IPresetManager<TKey, TValue>
    {
        /// <summary>
        /// Invoked when an element is saved or deleted
        /// </summary>
        event EventHandler PresetsCollectionChanged;

        /// <summary>
        /// Saves the <paramref name="value"/> using the <paramref name="name"/> as preset name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Save(TKey name, TValue value);

        /// <summary>
        /// Deletes the preset with the <paramref name="name"/> as preset name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Delete(TKey name);

        /// <summary>
        /// Loads and the returns the <see cref="TValue"/> named <paramref name="name"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Load(TKey name, out TValue value);

        /// <summary>
        /// Returns the list of all the saved keys
        /// </summary>
        /// <returns></returns>
        IEnumerable<TKey> GetKeys();
    }
}
