using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class FakeDictionary<T, U> : IEnumerable
{
    public List<FakeDictionaryEntry<T, U>> entries = new List<FakeDictionaryEntry<T, U>>();

    public U this[T key]
    {
        get
        {
            var entry = entries.FirstOrDefault(e => e.key.Equals(key));

            if (entry != null)
            {
                return entry.value;
            }

            else
            {
                throw new KeyNotFoundException($"Key '{key}' not found in the dictionary.");
            }
        }

        set
        {
            var entry = entries.FirstOrDefault(e => e.key.Equals(key));

            if (entry != null)
            {
                entry.value = value;
            }

            else
            {
                entries.Add(new FakeDictionaryEntry<T, U>(key, value));
            }
        }
    }

    public void Add(T key, U value)
    {
        if (ContainsKey(key))
        {
            throw new ArgumentException($"An element with the same key '{key}' already exists.");
        }

        entries.Add(new FakeDictionaryEntry<T, U>(key, value));
    }

    public bool ContainsKey(T key)
    {
        return entries.Any(e => e.key.Equals(key));
    }

    public IEnumerator<KeyValuePair<T, U>> GetEnumerator()
    {
        foreach (var entry in entries)
        {
            yield return new KeyValuePair<T, U>(entry.key, entry.value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count
    {
        get { return entries.Count; }
    }
}

[Serializable]
public class FakeDictionaryEntry<T, U>
{
    public T key;
    public U value;

    public FakeDictionaryEntry(T key, U value)
    {
        this.key = key;
        this.value = value;
    }
}