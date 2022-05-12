// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");



var kangaroo = new Kangaroo();
var rabit = new Rabit();

Copier.Copy<IJumpable>(kangaroo, rabit);

public static partial class Copier
{
    public static IJumpable Copy<TConstraint>(Rabit source, Kangaroo target) where TConstraint : IJumpable
    {
        target.JumpHeight = source.JumpHeight;
        return target;
    }

    public static Rabit Copy<TConstraint>(Rabit source, Kangaroo target) where TConstraint : Rabit
    {
        target.JumpHeight = source.JumpHeight;
        target.RunSpeed = source.RunSpeed;
        target.ThumpSound = source.ThumpSound;
        target.ThumpSounds = source.ThumpSounds;
        return target;
    }

    public static Kangaroo Copy<TConstraint>(Rabit source, Kangaroo target) where TConstraint : Kangaroo
    {
        target.JumpHeight = source.JumpHeight;
        target.PouchDimensions = source.PouchDimensions;
        return target;
    }
    public static Kangaroo Copy<TConstraint>(TConstraint source, TConstraint target) where TConstraint : Kangaroo
    {
        target.JumpHeight = source.JumpHeight;
        target.PouchDimensions = source.PouchDimensions;
        return target;
    }
    public static IJumpable Copy<TConstraint>(IJumpable source, IJumpable target) where TConstraint : IJumpable
    {
        target.JumpHeight = source.JumpHeight;
        return target;
    }
    public static IRunnable Copy<TConstraint>(IRunnable source, IRunnable target) where TConstraint : IRunnable
    {
        target.RunSpeed = source.RunSpeed;
        return target;
    }
}

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