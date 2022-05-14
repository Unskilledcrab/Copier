var test1 = new BillyGoat { RunSpeed = 10 };
var test2 = new Kangaroo { JumpHeight = 2 };

Copier.Copy<IRunnable>(test1, test2);
Copier.Copy<IRunnable>(test2, test1);
Copier.Copy<IJumpable>(new Pikachu(), new Charizard());
Copier.Copy<Pokemon>(new Pikachu(), new Charizard());
Copier.Copy<Kangaroo>(test2);
Copier.Copy<BillyGoat>(test1);

var pika = Copier.Copy<Pokemon>(new Pikachu());
var pika2 = Copier.Copy<Pikachu>(new Pikachu());
var pika3 = Copier.Copy<Charizard>(new Pikachu());

Console.ReadLine();

public class Pikachu : Pokemon, IJumpable
{
    public string Name { get; set; }
    public int JumpHeight { get; set; }
    public Pikachu Friend { get; set; }
}

public class Charizard : Pokemon, IJumpable
{
    public string Name { get; set; }
    public int JumpHeight { get; set; }
    public Pikachu Friend { get; set; }
}

public class Pokemon
{
    public int Index { get; set; }
}

public class Rabit : IJumpable
{
    public int JumpHeight { get; set; }
    public int RunSpeed { get; set; }
    public int ThumpSound { get; set; }
    public int ThumpSounds { get; set; }
    public Food food { get; set; }
    public Rabit Friend { get; set; }
    private int _boom { get; } = 0;
}

public class Food
{
    public int Calories { get; set; }
    public Vitamins VitaminB { get; set; }
}
public class Vitamins
{
    public int AmountOfNutrients { get; set; }
}




















public class BillyGoat : IRunnable
{
    public int RunSpeed { get; set; }
    public Rabit Rabit { get; set; }
    public int AnotherProperty { get; set; }
}


public class Kangaroo : IJumpable, IRunnable
{
    public int JumpHeight { get; set; }
    public int RunSpeed { get; set; }
    public int PouchDimensions { get; set; }
    public Rabit Rabit { get; set; }
    int _thinkPower = 0;

    public void test()
    {

    }
}

public interface IJumpable
{
    public int JumpHeight { get; set; }
}

public interface IRunnable
{
    public int RunSpeed { get; set; }
    public Rabit Rabit { get; set; }
}

public interface ITester
{
    public int Stuff { get; set; }
    public int MoreStuff { get; set; }
}