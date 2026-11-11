<style>
table {
  font-family: arial, sans-serif;
    border-collapse: collapse;
    width: 70%;
  }

  td, th {
    border: 1px solid #dddddd;
    text-align: left;
    padding: 8px;
  }

  tr:nth-child(even) {
    background-color: #dddddd;
  }
</style>
<h3>@member.Name</h3>
<p>Coffees: @coffees.Count</p>

<table>
    <th>Date</th>
    <th>Amount</th>
    @foreach (var date in @dates) {
        <tr>
            <td>@date.Key.Date.ToString("dd.MM.yyyy")</td>
            <td>@date.Value</td>
        </tr>
    }
</table>
<br>
<h5>Pay Infos</h5>
<table>
    <th>Date</th>
    <th>Amount</th>
    @foreach (var payInfo in @payDates) {
        <tr>
            <td>@payInfo.Created.Date.ToString("dd.MM.yyyy")</td>
            <td>@payInfo.Amount</td>
        </tr>
    }
</table>


<Chart Config="_config" @ref="_chart"></Chart>


@code {
    
    private const int InitalCount = 7;
    
    CancellationTokenSource pollingCancellationToken;
    [Parameter] public int Id { get; set; }
    public Member member { 
        get {
            return memberService.GetMember(Id);
        }
    }

    public List<Coffee> coffees {
        get {
            return coffeeService.GetCoffeesById(Id);
        }    
    }

    public Dictionary<DateTime, int> dates {
        get {
            return coffeeService.GetDailyCoffeeAmount(member);
        }    
    } 

    public List<PayInfo> payDates {
        get {
            return payInfoService.GetPayInfos(member);
        }    
    }

    void IDisposable.Dispose()
    {
        pollingCancellationToken?.Cancel();
    }

    private BarConfig _config;
    private Chart _chart;

    protected override void OnInitialized()
    {
        _config = new BarConfig
        {
            Options = new BarOptions
            {
                Responsive = true,
                Legend = new Legend
                {
                    Position = Position.Top
                },
                Title = new OptionsTitle
                {
                    Display = true,
                    Text = "ChartJs.Blazor Bar Chart"
                }
            }
        };

    var dateList = new List<DateTime>();
    foreach (DateTime date in coffeeService.GetDailyCoffeeAmount(member).Keys) {
        if (!dateList.Contains(date.Date))
            dateList.Add(date.Date);

    }

    foreach(PayInfo thisPayInfo in payInfoService.GetPayInfos(member))//.Where(x=> x.MemberId == member.Id).Select(x => x.Created.ToShortDateString())) {
    {

        if (!dateList.Contains(thisPayInfo.Created.Date))
            dateList.Add(thisPayInfo.Created.Date);
        

    }
    IDataset<TimePoint> dataset1 = new BarDataset<TimePoint>()//(coffeeService.GetDailyCoffeeAmount(member))//RandomScalingFactor(InitalCount))
        {
            Label = "Daily Amount Data",
            BackgroundColor = ColorUtil.FromDrawingColor(ChartColors.Red),
            BorderColor = ColorUtil.FromDrawingColor(ChartColors.Red)
        };
        foreach(var coffee in coffeeService.GetDailyCoffeeAmount(member)) {
            dataset1.Add(new TimePoint(coffee.Key, coffee.Value));
        }

        IDataset<TimePoint> dataset2 = new BarDataset<TimePoint>()//(payInfoService.GetPayInfos().Where(x => x.MemberId == member.Id).Select(x => (int)x.Amount).ToList())//RandomScalingFactor(InitalCount))
        {
            Label = "Pay Dates",
            BackgroundColor = ColorUtil.FromDrawingColor(ChartColors.Blue),
            BorderColor = ColorUtil.FromDrawingColor(ChartColors.Blue)
        };
        foreach(var payInfo in payInfoService.GetPayInfos(member)) {
            dataset2.Add(new TimePoint(payInfo.Created, payInfo.Amount));
        }
        foreach(var date in dateList.Order<DateTime>().ToList()) {
            _config.Data.Labels.Add(date.ToShortDateString());
            if (dataset1.Where(x => x.Time.Date == date).SingleOrDefault() != null) {
            }
        }
        _config.Data.Datasets.Add(dataset1);
        _config.Data.Datasets.Add(dataset2);

        _config.Options.Scales.XAxes = 
        
        Console.WriteLine(dataset1.Count());
        Console.WriteLine(dataset2.Count());
    } 

} *@
