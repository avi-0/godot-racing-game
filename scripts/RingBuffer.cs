using System;

namespace racingGame;

public class RingBuffer<T>(int capacity)
{
	private T[] _buffer = new T[capacity];
	private int _head = 0;
	private int _tail = 0;
	private bool _isFull = false;

	public void Add(T item)
	{
		if (_tail == _head && _isFull)
		{
			_head = (_head + 1) % _buffer.Length;
		}
		
		_buffer[_tail] = item;
		_tail = (_tail + 1) % _buffer.Length;
		
		if (_tail == _head)
		{
			_isFull = true;
		}
	}

	public int Count
	{
		get
		{
			if (_isFull)
				return capacity;
			
			return (_tail - _head) % capacity;
		}
	}
	
	public T this[int index] => _buffer[(_head + index) % _buffer.Length];
}