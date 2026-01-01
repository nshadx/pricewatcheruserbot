namespace pricewatcheruserbot.Workers;

public class SimpleMovingAverageState
{
    public int K { get; set; }
    public int CurrentK { get; set; }
    public double[] Values { get; set; } = [];
    public int Index { get; set; }
    public double Sum { get; set; }
    public double Sma { get; set; }
    public double Previous { get; set; }
}

public class SimpleMovingAverage
{
    private readonly int _k;
    private readonly double[] _values;

    private int _index;
    private double _sum;
    private int _currentK;
    private double _sma;

    public double Previous { get; private set; }
    
    public bool TryGetLatestValue(out double value)
    {
        var isReady = _currentK == _k; 
        if (isReady)
        {
            value = _sma;
            return true;
        }

        value = default;
        return false;
    }
    
    public SimpleMovingAverage(int k)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        _k = k;
        _values = new double[_k];
    }

    public SimpleMovingAverage(SimpleMovingAverageState state)
    {
        _currentK = state.CurrentK;
        _k = state.K;
        _values = state.Values;
        _index = state.Index;
        _sma = state.Sma;
        _sum = state.Sum;
        Previous = state.Previous;
    }
    
    public void Update(double nextInput)
    {
        if (_currentK < _k)
        {
            _currentK++;
        }
        
        _sum = _sum - _values[_index] + nextInput;
        
        Previous = _values[_k - 1];
        
        _values[_index] = nextInput;
        
        _index = (_index + 1) % _k;

        _sma = _sum / _k;
    }

    public SimpleMovingAverageState Save()
    {
        return new SimpleMovingAverageState
        {
            K = _k,
            CurrentK = _currentK,
            Index = _index,
            Sum = _sum,
            Sma = _sma,
            Previous = Previous,
            Values = _values
        };
    }
}