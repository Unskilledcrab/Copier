var test1 = new BillyGoat { RunSpeed = 10 };
var test2 = new Kangaroo { JumpHeight = 2 };

Copier.Copy<IRunnable>(test1, test2);
Copier.Copy<IRunnable>(test1, test2);
Copier.Copy<Kangaroo>(test2);


public class Rabit : IJumpable
{
    public int JumpHeight { get; set; }
    public int RunSpeed { get; set; }
    public int ThumpSound { get; set; }
    public int ThumpSounds { get; set; }
    public int test { get; set; }
    public Food food { get; set; }
    public Rabit Friend { get; set; }
    private int _boom { get; } = 0;
}

public class Food
{
    public int test { get; set; }
    public Vitamins VitaminB { get; set; }
}
public class Vitamins
{
    public int test { get; set; }
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