interface IHazardNotifier
{
    void NotifyHazard(string message);
}

class OverfillException : Exception
{
    public OverfillException(string message) : base(message) { }
}

abstract class Container
{
    private static int counter = 1;
    public string SerialNumber { get; }
    public double CargoWeight { get; private set; }
    public double MaxCapacity { get; }
    public double OwnWeight { get; }
    public double Height { get; }
    public double Depth { get; }

    protected Container(string type, double maxCapacity, double ownWeight, double height, double depth)
    {
        SerialNumber = $"KON-{type}-{counter++}";
        MaxCapacity = maxCapacity;
        OwnWeight = ownWeight;
        Height = height;
        Depth = depth;
        CargoWeight = 0;
    }

    public virtual void LoadCargo(double weight)
    {
        if (CargoWeight + weight > MaxCapacity)
            throw new OverfillException($"Przekroczono maksymalną ładowność kontenera {SerialNumber}");
        CargoWeight += weight;
    }

    public virtual void UnloadCargo()
    {
        CargoWeight = 0;
    }

    public override string ToString()
    {
        return $"{SerialNumber} | Waga ładunku: {CargoWeight}/{MaxCapacity} kg";
    }
}

class LiquidContainer : Container, IHazardNotifier
{
    public bool IsHazardous { get; }

    public LiquidContainer(double maxCapacity, double ownWeight, double height, double depth, bool isHazardous)
        : base("L", maxCapacity, ownWeight, height, depth)
    {
        IsHazardous = isHazardous;
    }

    public override void LoadCargo(double weight)
    {
        double limit = IsHazardous ? MaxCapacity * 0.5 : MaxCapacity * 0.9;
        if (CargoWeight + weight > limit)
        {
            NotifyHazard($"Niebezpieczna próba załadunku kontenera {SerialNumber}!");
            throw new OverfillException($"Przekroczono limit załadunku dla {SerialNumber}");
        }
        base.LoadCargo(weight);  
    }

    public void NotifyHazard(string message)
    {
        Console.WriteLine($"!!! ALERT: {message}");
    }
}

class GasContainer : Container, IHazardNotifier
{
    public double Pressure { get; }

    public GasContainer(double maxCapacity, double ownWeight, double height, double depth, double pressure)
        : base("G", maxCapacity, ownWeight, height, depth)
    {
        Pressure = pressure;
    }

    public override void UnloadCargo()
    {
        base.LoadCargo(-CargoWeight * 0.95); 
    }

    public void NotifyHazard(string message)
    {
        Console.WriteLine($"!!! ALERT: {message}");
    }
}

class RefrigeratedContainer : Container
{
    public string ProductType { get; }
    public double Temperature { get; }

    public RefrigeratedContainer(double maxCapacity, double ownWeight, double height, double depth, string productType, double temperature)
        : base("C", maxCapacity, ownWeight, height, depth)
    {
        ProductType = productType;
        Temperature = temperature;
    }

    public override string ToString()
    {
        return base.ToString() + $" | Produkt: {ProductType} | Temp: {Temperature}°C";
    }
}

class Ship
{
    public string Name { get; }
    public int MaxContainers { get; }
    public double MaxWeight { get; }
    public double Speed { get; }
    private List<Container> containers;

    public Ship(string name, int maxContainers, double maxWeight, double speed)
    {
        Name = name;
        MaxContainers = maxContainers;
        MaxWeight = maxWeight;
        Speed = speed;
        containers = new List<Container>();
    }

    public void LoadContainer(Container container)
    {
        if (containers.Count >= MaxContainers)
            throw new InvalidOperationException("Statek osiągnął maksymalną liczbę kontenerów!");
        if (TotalWeight() + container.OwnWeight + container.CargoWeight > MaxWeight * 1000)
            throw new InvalidOperationException("Przekroczona maksymalna waga kontenerów na statku!");
        containers.Add(container);
    }

    public void RemoveContainer(string serialNumber)
    {
        var container = containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container != null)
        {
            containers.Remove(container);
            // Dodanie kontenera z powrotem do ogólnej listy kontenerów
            Program.containers.Add(container);  // Program.containers to lista kontenerów w głównym programie
            Console.WriteLine("\nKontener usunięty ze statku i dodany z powrotem do listy kontenerów.");
        }
        else
        {
            Console.WriteLine("\nNie znaleziono kontenera na statku.");
        }
    }

    public void TransferContainer(Ship otherShip, string serialNumber)
    {
        var container = containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container == null) return;
        otherShip.LoadContainer(container);
        containers.Remove(container);
    }

    private double TotalWeight()
    {
        return containers.Sum(c => c.OwnWeight + c.CargoWeight);
    }

    public override string ToString()
    {
        return $"Statek: {Name} | Prędkość: {Speed} węzłów | Maks. kontenerów: {MaxContainers} | Ładowność: {MaxWeight} ton | Liczba kontenerów: {containers.Count}";
    }

    public void PrintContainers()
    {
        foreach (var container in containers)
        {
            Console.WriteLine(container);
        }
    }
}

class Program
{
    public static List<Ship> ships = new List<Ship>();
    public static List<Container> containers = new List<Container>();

    static void Main()
    {
        while (true)
        {
            Console.Clear();
            DisplayMainMenu();
            string choice = Console.ReadLine();
            HandleMainMenuChoice(choice);
        }
    }

    static void DisplayMainMenu()
    {
        Console.WriteLine("Lista kontenerowców:");
        if (ships.Count == 0)
            Console.WriteLine("Brak");
        else
            ships.ForEach(s => Console.WriteLine(s));

        Console.WriteLine("\nLista kontenerów:");
        if (containers.Count == 0)
            Console.WriteLine("Brak");
        else
            containers.ForEach(c => Console.WriteLine(c));

        Console.WriteLine("\nMożliwe akcje:");
        Console.WriteLine("1. Dodaj kontenerowiec");
        if (ships.Count > 0)  // Opcje kontenerów pojawiają się tylko, jeśli jest statek
        {
            Console.WriteLine("2. Usuń kontenerowiec");
            Console.WriteLine("3. Dodaj kontener");
            Console.WriteLine("4. Załaduj kontener na statek");
            Console.WriteLine("5. Usuń kontener ze statku");
            Console.WriteLine("6. Przenieś kontener między statkami");
        }
        Console.WriteLine("7. Wyjdź");

        Console.Write("\nWybierz opcję: ");
    }

    static void HandleMainMenuChoice(string choice)
    {
        switch (choice)
        {
            case "1":
                AddShip();
                break;
            case "2":
                if (ships.Count > 0) RemoveShip();
                break;
            case "3":
                if (ships.Count > 0) AddContainer();
                break;
            case "4":
                if (ships.Count > 0) LoadContainerOntoShip();
                break;
            case "5":
                if (ships.Count > 0) RemoveContainerFromShip();
                break;
            case "6":
                if (ships.Count > 0) TransferContainerBetweenShips();
                break;
            case "7":
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("Nieprawidłowy wybór. Naciśnij Enter, aby kontynuować...");
                Console.ReadLine();
                break;
        }
    }

    static void AddShip()
    {
        Console.Clear();
        Console.Write("Podaj nazwę statku: ");
        string name = Console.ReadLine();
        Console.Write("Podaj maksymalną liczbę kontenerów: ");
        int maxContainers = int.Parse(Console.ReadLine());
        Console.Write("Podaj maksymalną wagę (w tonach): ");
        double maxWeight = double.Parse(Console.ReadLine());
        Console.Write("Podaj prędkość (węzły): ");
        double speed = double.Parse(Console.ReadLine());

        ships.Add(new Ship(name, maxContainers, maxWeight, speed));
        Console.WriteLine("\nDodano statek! Naciśnij Enter, aby kontynuować...");
        Console.ReadLine();
    }

    static void RemoveShip()
    {
        Console.Clear();
        Console.Write("Podaj nazwę statku do usunięcia: ");
        string name = Console.ReadLine();
        var ship = ships.FirstOrDefault(s => s.Name == name);
        if (ship != null)
        {
            ships.Remove(ship);
            Console.WriteLine("\nStatek usunięty.");
        }
        else
        {
            Console.WriteLine("\nNie znaleziono statku.");
        }
        Console.ReadLine();
    }

    static void AddContainer()
    {
        Console.Clear();
        Console.WriteLine("Wybierz typ kontenera:");
        Console.WriteLine("1. Płynny (L)");
        Console.WriteLine("2. Gazowy (G)");
        Console.WriteLine("3. Chłodniczy (C)");
        Console.Write("\nTwój wybór: ");
        string type = Console.ReadLine();

        Console.Write("Podaj maksymalną pojemność (kg): ");
        double maxCapacity = double.Parse(Console.ReadLine());
        Console.Write("Podaj wagę własną kontenera (kg): ");
        double ownWeight = double.Parse(Console.ReadLine());
        Console.Write("Podaj wysokość (cm): ");
        double height = double.Parse(Console.ReadLine());
        Console.Write("Podaj głębokość (cm): ");
        double depth = double.Parse(Console.ReadLine());

        switch (type)
        {
            case "1":
                Console.Write("Czy ładunek jest niebezpieczny? (tak/nie): ");
                bool isHazardous = Console.ReadLine().ToLower() == "tak";
                containers.Add(new LiquidContainer(maxCapacity, ownWeight, height, depth, isHazardous));
                break;
            case "2":
                Console.Write("Podaj ciśnienie (atm): ");
                double pressure = double.Parse(Console.ReadLine());
                containers.Add(new GasContainer(maxCapacity, ownWeight, height, depth, pressure));
                break;
            case "3":
                Console.Write("Podaj rodzaj przechowywanego produktu: ");
                string product = Console.ReadLine();
                Console.Write("Podaj temperaturę przechowywania: ");
                double temperature = double.Parse(Console.ReadLine());
                containers.Add(new RefrigeratedContainer(maxCapacity, ownWeight, height, depth, product, temperature));
                break;
            default:
                Console.WriteLine("Nieprawidłowy wybór.");
                break;
        }

        Console.WriteLine("\nKontener dodany! Naciśnij Enter, aby kontynuować...");
        Console.ReadLine();
    }

    static void LoadContainerOntoShip()
    {
        Console.Clear();
        Console.Write("Podaj numer seryjny kontenera: ");
        string serial = Console.ReadLine();
        Console.Write("Podaj nazwę statku: ");
        string shipName = Console.ReadLine();

        var container = containers.FirstOrDefault(c => c.SerialNumber == serial);
        var ship = ships.FirstOrDefault(s => s.Name == shipName);

        if (container != null && ship != null)
        {
            try
            {
                ship.LoadContainer(container);
                containers.Remove(container);
                Console.WriteLine("\nKontener załadowany na statek.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nBłąd: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("\nNie znaleziono statku lub kontenera.");
        }
        Console.ReadLine();
    }

    static void RemoveContainerFromShip()
    {
        Console.Clear();
        Console.Write("Podaj nazwę statku: ");
        string shipName = Console.ReadLine();
        Console.Write("Podaj numer seryjny kontenera do usunięcia: ");
        string serial = Console.ReadLine();

        var ship = ships.FirstOrDefault(s => s.Name == shipName);
        if (ship != null)
        {
            ship.RemoveContainer(serial);
            Console.WriteLine("\nKontener usunięty ze statku.");
        }
        else
        {
            Console.WriteLine("\nNie znaleziono statku.");
        }
        Console.ReadLine();
    }

    static void TransferContainerBetweenShips()
    {
        Console.Clear();
        Console.Write("Podaj nazwę statku źródłowego: ");
        string fromShipName = Console.ReadLine();
        Console.Write("Podaj nazwę statku docelowego: ");
        string toShipName = Console.ReadLine();
        Console.Write("Podaj numer seryjny kontenera: ");
        string serial = Console.ReadLine();

        var fromShip = ships.FirstOrDefault(s => s.Name == fromShipName);
        var toShip = ships.FirstOrDefault(s => s.Name == toShipName);

        if (fromShip != null && toShip != null)
        {
            fromShip.TransferContainer(toShip, serial);
            Console.WriteLine("\nKontener przeniesiony.");
        }
        else
        {
            Console.WriteLine("\nNie znaleziono statku.");
        }
        Console.ReadLine();
    }
}



