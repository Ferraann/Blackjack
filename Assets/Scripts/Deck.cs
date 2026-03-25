using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
    public GameObject dealer;
    public GameObject player;
    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Text finalMessage;
    public Text probMessage;

    public Text bankText;
    public TMP_Dropdown betDropdown;
    private int bank = 1000;
    private int currentBet = 0;

    public int[] values = new int[52];
    int cardIndex = 0;    
       
    private void Awake()
    {    
        InitCardValues();        

    }

    private void Start()
    {
        UpdateBankUI();
        ShuffleCards();
        StartGame();
    }

    private void InitCardValues()
    {
        /*TODO:
         * Asignar un valor a cada una de las 52 cartas del atributo "values".
         * En principio, la posición de cada valor se deberá corresponder con la posición de faces. 
         * Por ejemplo, si en faces[1] hay un 2 de corazones, en values[1] debería haber un 2.
         */

        for (int i = 0; i < 52; i++)
        {
            // Obtenemos un valor cíclico del 0 al 12 para representar las 13 cartas de cada palo
            int cardRank = i % 13;

            if (cardRank == 0)
            {
                // Asumiendo que la primera carta del palo (posición 0, 13, 26, 39) es el As
                values[i] = 11;
            }
            else if (cardRank >= 1 && cardRank <= 9)
            {
                // Las cartas del 2 al 10. Como el array empieza en 0, le sumamos 1.
                // Ejemplo: si cardRank es 1 (que sería el 2), su valor será 1 + 1 = 2.
                values[i] = cardRank + 1;
            }
            else
            {
                // Las figuras restantes (J, Q, K) que ocupan los valores 10, 11 y 12 del ciclo.
                values[i] = 10;
            }
        }
    }

    private void ShuffleCards()
{
    /*TODO:
     * Barajar las cartas aleatoriamente.
     * El método Random.Range(0,n), devuelve un valor entre 0 y n-1
     * Si lo necesitas, puedes definir nuevos arrays.
     */
     
    // Recorremos las 52 posiciones de la baraja
    for (int i = 0; i < 52; i++)
    {
        // Elegimos un índice aleatorio entre la posición actual (i) y el final (52 excluido)
        // Unity Random.Range(int min, int max) incluye el min pero excluye el max.
        int randomIndex = Random.Range(i, 52);

        // 1. Intercambiamos la imagen de la carta (Sprite en el array 'faces')
        Sprite tempFace = faces[i];
        faces[i] = faces[randomIndex];
        faces[randomIndex] = tempFace;

        // 2. Intercambiamos el valor de la carta (int en el array 'values')
        int tempValue = values[i];
        values[i] = values[randomIndex];
        values[randomIndex] = tempValue;
    }
}

    void StartGame()
    {
        // --- NUEVO: Gestión de la apuesta inicial ---
        // Leemos el valor seleccionado en el Dropdown
        int betValue = int.Parse(betDropdown.options[betDropdown.value].text);

        if (bank >= betValue)
        {
            currentBet = betValue;
            bank -= currentBet; // Restamos la apuesta de la banca al empezar
            UpdateBankUI();
            betDropdown.interactable = false; // Bloqueamos el dropdown mientras se juega
        }
        else
        {
            finalMessage.text = "Saldo insuficiente para apostar.";
            hitButton.interactable = false;
            stickButton.interactable = false;
            return; // Cortamos la ejecución, no se reparten cartas
        }
        // -------------------------------------------

        // Repartimos dos cartas a cada uno
        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        if (playerPoints == 21 || dealerPoints == 21)
        {
            dealer.GetComponent<CardHand>().InitialToggle();
            hitButton.interactable = false;
            stickButton.interactable = false;

            if (playerPoints == 21 && dealerPoints == 21)
            {
                finalMessage.text = "¡Empate a Blackjack!";
                bank += currentBet; // Recupera la apuesta
            }
            else if (playerPoints == 21)
            {
                finalMessage.text = "¡Blackjack! Has ganado.";
                bank += currentBet * 2; // Gana el doble 
            }
            else
            {
                finalMessage.text = "El Dealer tiene Blackjack. Has perdido.";
        }

            UpdateBankUI();
            betDropdown.interactable = true; // Volvemos a activar el dropdown para la siguiente
        }
    }

    private void CalculateProbabilities()
    {
        /*TODO:
         * Calcular las probabilidades de:
         * - Teniendo la carta oculta, probabilidad de que el dealer tenga más puntuación que el jugador
         * - Probabilidad de que el jugador obtenga entre un 17 y un 21 si pide una carta
         * - Probabilidad de que el jugador obtenga más de 21 si pide una carta          
         */

        // 1. Obtenemos los puntos actuales del jugador
        int playerPoints = player.GetComponent<CardHand>().points;

        // 2. Calculamos los puntos VISIBLES del dealer (ignorando su carta [0] que está boca abajo)
        int dealerVisiblePoints = 0;
        int dealerAces = 0;
        var dealerCards = dealer.GetComponent<CardHand>().cards;

        if (dealerCards.Count > 1)
        {
            for (int i = 1; i < dealerCards.Count; i++)
            {
                int val = dealerCards[i].GetComponent<CardModel>().value;
                if (val == 11) dealerAces++;
                else dealerVisiblePoints += val;
            }
            // Si el dealer tiene Ases visibles, ajustamos su valor (1 o 11) para no pasarnos de 21
            for (int i = 0; i < dealerAces; i++)
            {
                if (dealerVisiblePoints + 11 <= 21) dealerVisiblePoints += 11;
                else dealerVisiblePoints += 1;
            }
        }

        // 3. Recopilamos todas las cartas que el jugador NO ha visto en una lista
        List<int> unseenValues = new List<int>();
        if (dealerCards.Count > 0)
        {
            unseenValues.Add(dealerCards[0].GetComponent<CardModel>().value); // La carta oculta del dealer
        }
        for (int i = cardIndex; i < 52; i++)
        {
            unseenValues.Add(values[i]); // El resto de la baraja que no se ha repartido
        }

        // 4. Variables para contar cuántas cartas cumplen nuestras condiciones
        int dealerHigherCount = 0;
        int player17to21Count = 0;
        int playerOver21Count = 0;

        // 5. Evaluamos una por una todas las cartas misteriosas
        foreach (int cardVal in unseenValues)
        {
            // --- Cálculo para el Dealer ---
            // ¿Qué pasaría si esta carta misteriosa fuera la carta oculta del dealer?
            int simulatedDealerPoints = dealerVisiblePoints + cardVal;
            if (cardVal == 11 && dealerVisiblePoints + 11 > 21)
            {
                simulatedDealerPoints = dealerVisiblePoints + 1; // El As vale 1 si se pasa
            }

            // El dealer gana si tiene más puntos que el jugador, pero sin pasarse de 21
            if (simulatedDealerPoints > playerPoints && simulatedDealerPoints <= 21)
            {
                dealerHigherCount++;
            }

            // --- Cálculo para el Jugador ---
            // ¿Qué pasaría si el jugador pide carta y le sale esta carta misteriosa?
            int simulatedPlayerPoints = playerPoints + cardVal;
            if (cardVal == 11 && playerPoints + 11 > 21)
            {
                simulatedPlayerPoints = playerPoints + 1; // El As vale 1 si se pasa
            }

            if (simulatedPlayerPoints >= 17 && simulatedPlayerPoints <= 21)
            {
                player17to21Count++;
            }
            else if (simulatedPlayerPoints > 21)
            {
                playerOver21Count++;
            }
        }

        // 6. Calculamos la probabilidad final (casos favorables / casos totales)
        float totalUnseen = unseenValues.Count;
        float probDealerHigher = totalUnseen > 0 ? dealerHigherCount / totalUnseen : 0;
        float prob17to21 = totalUnseen > 0 ? player17to21Count / totalUnseen : 0;
        float probOver21 = totalUnseen > 0 ? playerOver21Count / totalUnseen : 0;

        // 7. Lo mostramos en la interfaz de usuario con 2 decimales ("F4" mostraría 4 decimales como en el PDF)
        probMessage.text = "Deal > Play: " + probDealerHigher.ToString("F4") + "\n" +
                           "17<=X<=21: " + prob17to21.ToString("F4") + "\n" +
                           "X>21: " + probOver21.ToString("F4");
    }

    void PushDealer()
    {
        /*TODO:
         * Dependiendo de cómo se implemente ShuffleCards, es posible que haya que cambiar el índice.
         */
        dealer.GetComponent<CardHand>().Push(faces[cardIndex],values[cardIndex]);
        cardIndex++;        
    }

    void PushPlayer()
    {
        /*TODO:
         * Dependiendo de cómo se implemente ShuffleCards, es posible que haya que cambiar el índice.
         */
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]/*,cardCopy*/);
        cardIndex++;
        CalculateProbabilities();
    }

    public void Hit()
    {
        /*TODO: 
         * Si estamos en la mano inicial, debemos voltear la primera carta del dealer.
         * (NOTA: Ignoramos esto porque según la Figura 2 del PDF y las reglas, la carta sigue oculta al hacer Hit).
         */

        // Repartimos carta al jugador
        PushPlayer();

        /*TODO:
         * Comprobamos si el jugador ya ha perdido y mostramos mensaje
         */
        // Accedemos a los puntos actuales del jugador
        if (player.GetComponent<CardHand>().points > 21)
        {
            // Revelamos la carta oculta del dealer porque la partida ha terminado
            dealer.GetComponent<CardHand>().InitialToggle();

            // Bloqueamos los botones
            hitButton.interactable = false;
            stickButton.interactable = false;

            // Mostramos el mensaje de derrota
            finalMessage.text = "¡Te has pasado de 21! Has perdido.";

            // Volvemos a activar apuestas
            betDropdown.interactable = true;
        }
    }

    public void Stand()
    {
        dealer.GetComponent<CardHand>().InitialToggle();
        hitButton.interactable = false;
        stickButton.interactable = false;

        CardHand dealerHand = dealer.GetComponent<CardHand>();
        CardHand playerHand = player.GetComponent<CardHand>();

        while (dealerHand.points <= 16)
        {
            PushDealer();
        }

        int pPoints = playerHand.points;
        int dPoints = dealerHand.points;

        if (dPoints > 21)
        {
            finalMessage.text = "El Dealer se ha pasado de 21. ¡Has ganado!";
            bank += currentBet * 2; // Pago doble 
        }
        else if (dPoints > pPoints)
        {
            finalMessage.text = "El Dealer tiene más puntos. ¡Has perdido!";
    }
        else if (dPoints < pPoints)
        {
            finalMessage.text = "Tienes más puntos que el Dealer. ¡Has ganado!";
            bank += currentBet * 2; // Pago doble 
        }
        else
        {
            finalMessage.text = "Habéis empatado.";
            bank += currentBet; // Se devuelve la apuesta
        }

        UpdateBankUI();

        // Volvemos a activar apuestas
        betDropdown.interactable = true; 
    }

    public void PlayAgain()
    {
        hitButton.interactable = true;
        stickButton.interactable = true;
        finalMessage.text = "";
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();          
        cardIndex = 0;
        ShuffleCards();
        StartGame();
    }

    // Opcional:
    private void UpdateBankUI()
    {
        if (bankText != null)
        {
            bankText.text = "Credito: " + bank.ToString();
        }
    }

}
