using System;
using System.Collections.Generic;

// NOTE: This exists purely so we can have a ref indexer (and therefore don't need to do a manual
// read-modify-write pattern everytime we want to change a field in some object in the list).
public class RefList<T>
{
	public int count;
	public int capacity;
	public T[] items;

	public ref T this[int index]
	{
		get { return ref items[index]; }
	}

	public int Count { get { return count; } }

	public RefList(int capacity)
	{
		count = 0;
		this.capacity = capacity;
		items = new T[capacity];
	}

	public ref T Add()
	{
		if (count == capacity)
		{
			capacity *= 2;
			Array.Resize(ref items, capacity);
		}
		count += 1;
		return ref items[count - 1];
	}

	public ref T Add(T item)
	{
		ref T newItem = ref Add();
		newItem = item;
		return ref newItem;
	}

	public void Clear()
	{
		Array.Clear(items, 0, count);
		count = 0;
	}

	public bool Remove(ref T item)
	{
		for (int i = 0; i < count; i++)
		{
			// Seriously, C#?
			if (EqualityComparer<T>.Default.Equals(items[i], item))
			{
				Array.Copy(items, i, items, i + 1, count - i);
				return true;
			}
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		Array.Copy(items, index, items, index + 1, count - index);
	}
}