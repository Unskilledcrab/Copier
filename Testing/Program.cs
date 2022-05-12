var test1 = new Rabit { JumpHeight = 2 };
var test2 = new Kangaroo { JumpHeight = 2 };

Copier.Copy<IJumpable>(test1, test2);
Copier.Copy<IRunnable>(new Rabit { JumpHeight = 2 }, test2);
 
Copier.Copy<ITester>(test1, test2);
Copier.Copy<ITester>(test1, test2);

Copier.Copy<Rabit>(test1);

public class Kangaroo : IJumpable, IRunnable
{
    public int JumpHeight { get; set; }
    public int RunSpeed { get; set; }
    public int PouchDimensions { get; set; }
    int _thinkPower = 0;
}

public class Rabit : IJumpable, IRunnable
{
    public int JumpHeight { get; set; } 
    public int RunSpeed { get; set; }
    public int ThumpSound { get; set; }
    public int ThumpSounds { get; set; }
    public int test { get; set; }
    private int _boom { get; } = 0;
}   
 
public interface IJumpable
{ 
    public int JumpHeight { get; set; } 
}

public interface IRunnable
{
    public int RunSpeed { get; set; }
}

public interface ITester
{
    public int Stuff { get; set; }
    public int MoreStuff { get; set; }
}