namespace VirtualRoulette;
using ge.singular.roulette;


public static class something
{
    public static void something1()
    {
        //bet string received from the client 
        string bet = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]"; 

//check bet string’s validity 
        IsBetValidResponse ibvr = CheckBets.IsValid(bet); 

//as a response you will receive whether bet string is correct or not, and bet amount made by user (in cents). 
        Console.WriteLine("Is bet valid: " + ibvr.getIsValid() + " bet amount in cents is: " + ibvr.getBetAmount()); 
    }
}