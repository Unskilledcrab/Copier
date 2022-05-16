var pikachu = new Pikachu() 
{ 
    Name = "pika pika", 
    PokaIndex = 1, 
    Speed = 1000
};
pikachu.PikaPal = pikachu; // So lonely :(

var charizard = new Charizard() 
{ 
    Name = "Destroyer", 
    PokaIndex = 2, 
    Friend = pikachu, 
    JumpHeight = 2 
};


Console.WriteLine(charizard);
Console.WriteLine(pikachu);
Copier.Copy<IPokemon>(pikachu, charizard);
Console.WriteLine(charizard);
Console.WriteLine(pikachu);

Console.WriteLine(Copier.Copy<Pokemon>(pikachu));

Console.ReadLine();

public class Pikachu : Pokemon
{
    public int Speed { get; set; }
    public Pikachu PikaPal { get; set; }
    public void ScratchAttack()
    {
        // I Attack you
    }
    public override string ToString()
    {
        return $"{base.ToString()} | Pikachu is friends with {PikaPal.Name}";
    }
}

public class Charizard : Pokemon
{
    public int JumpHeight { get; set; }
    public Pikachu Friend { get; set; }
    public override string ToString()
    {
        return $"{base.ToString()} | Charizard is friends with {Friend.Name}";
    }
}

public class Pokemon : IPokemon
{
    public int PokaIndex { get; set; }
    public string Name { get; set; }
    public override string ToString()
    {
        return $"{PokaIndex}: {Name}";
    }
}

public interface IPokemon
{
    public int PokaIndex { get; set; }
    public string Name { get; set; }
}