namespace JsonReader.ViewModels;

public class MainViewModel : DependencyObject
{
    public const int carCount = 500; 
    
    public Dispatcher MyDispatcher { get; set; } = Dispatcher.CurrentDispatcher;
    public CancellationTokenSource CancellationToken { get; set; } = new();

    public RelayCommand StartOperationCommand { get; set; }
    public RelayCommand CancelOperationCommand { get; set; }

    public ObservableCollection<Car> Cars
    {
        get { return (ObservableCollection<Car>)GetValue(CarsProperty); }
        set { SetValue(CarsProperty, value); }
    }
    public static readonly DependencyProperty CarsProperty =
        DependencyProperty.Register("Cars", typeof(ObservableCollection<Car>), typeof(MainViewModel));

    public string Time
    {
        get { return (string)GetValue(TimeProperty); }
        set { SetValue(TimeProperty, value); }
    }
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register("Time", typeof(string), typeof(MainViewModel));

    public Stopwatch StopWatch { get; set; } = new();

    public bool IsMultiThreadOperation { get; set; } = false;
    public bool ThreadStarted { get; set; } = false;


    static MainViewModel()
    {
        if (Directory.Exists("Cars"))
        {
            if (Directory.GetFiles($@"{Environment.CurrentDirectory}\Cars").Length == carCount) return;
            else
            {
                foreach (var item in Directory.GetFiles($@"{Environment.CurrentDirectory}\Cars")) File.Delete(item);

                Directory.Delete($@"{Environment.CurrentDirectory}\Cars");
            }
        }
        
        if (!Directory.Exists("Cars")) Directory.CreateDirectory("Cars");

        var carGenerator = new Faker<Car>()
            .RuleFor(c => c.Model, f => f.Vehicle.Model())
            .RuleFor(c => c.Vendor, f => f.Vehicle.Manufacturer())
            .RuleFor(c => c.Year, f => f.Random.Number(2010, 2020))
            .RuleFor(c => c.ImagePath, f => f.Image.Transport());

        foreach (var item in carGenerator.Generate(carCount))
        {
            var jsonString = JsonConvert.SerializeObject(item, Formatting.Indented);
            File.WriteAllText($@"{Environment.CurrentDirectory}\Cars\{item.Id}.json", jsonString);
        }
    }

    public MainViewModel()
    {
        StartOperationCommand = new((sender) => StartOperation(), (sender) => !ThreadStarted);
        CancelOperationCommand = new((sender) => CancellationToken.Cancel(), (sender) => ThreadStarted);

        Cars = new();
        Time = "00:00:00:000000";
    }


    public void StartOperation()
    {
        Cars.Clear();
        Time = "00:00:00:000000";
        
        ThreadStarted = true;
        StopWatch.Start();

        if (IsMultiThreadOperation)
        {
            foreach (var item in Directory.GetFiles($@"{Environment.CurrentDirectory}\Cars"))
            {
                ThreadPool.QueueUserWorkItem(MultiThreadOperation, item);
            }
        }
        else
        {
            ThreadPool.QueueUserWorkItem(SingleThreadOperation);
        }
    }


    public void SingleThreadOperation(object? state)
    {
        foreach (var item in Directory.GetFiles($@"{Environment.CurrentDirectory}\Cars"))
        {
            if (item.EndsWith(".json"))
            {
                var jsonString = File.ReadAllText(item);
                var car = JsonConvert.DeserializeObject<Car>(jsonString);

                if (CancellationToken.Token.IsCancellationRequested)
                {
                    MyDispatcher.Invoke(() => Cars.Clear());
                    Reset();
                    return;
                }

                if (car is not null) MyDispatcher.Invoke(() => Cars.Add(car));
            }

            if (CancellationToken.Token.IsCancellationRequested)
            {
                MyDispatcher.Invoke(() => Cars.Clear());
                Reset();
                return;
            }
        }

        Reset();
    }

    public void MultiThreadOperation(object? path)
    {
        lock (MyDispatcher.Invoke(() => Cars))
        {
            if (!ThreadStarted) return;

            var jsonString = File.ReadAllText(path.ToString());
            var car = JsonConvert.DeserializeObject<Car>(jsonString);

            if (CancellationToken.Token.IsCancellationRequested)
            {
                MyDispatcher.Invoke(() => Cars.Clear());
                Reset();
                return;
            }

            if (car is not null) MyDispatcher.Invoke(() => Cars.Add(car));
        }
        
        if (MyDispatcher.Invoke(() => Cars).Count == carCount) Reset();
    }


    public void Reset()
    {
        ThreadStarted = false;

        StopWatch.Stop();
        MyDispatcher.Invoke(() => Time = TimeSpan.FromMilliseconds(StopWatch.ElapsedMilliseconds).ToString());

        CancellationToken = new();
        StopWatch = new();
    }
}
