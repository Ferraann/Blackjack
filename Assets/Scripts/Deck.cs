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
        // Estado inicial: bloqueamos todo hasta que pulse Play
        hitButton.interactable = false;
        stickButton.interactable = false;
        finalMessage.text = "Elige apuesta y pulsa Play";
        UpdateBankUI();

        // Eliminamos ShuffleCards() y StartGame() de aquí
    }

    private void InitCardValues()
    {
        for (int i = 0; i < 52; i++)
        {
            int cardRank = i % 13;

            if (cardRank == 0) values[i] = 11;
            else if (cardRank >= 1 && cardRank <= 9) values[i] = cardRank + 1;
            else values[i] = 10;
        }
    }

    private void ShuffleCards()
    {
        for (int i = 0; i < 52; i++)
        {
            int randomIndex = Random.Range(i, 52);
            Sprite tempFace = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempFace;
            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;
        }
    }

    void StartGame()
    {
        // Gestión de la apuesta inicial
        int betValue = int.Parse(betDropdown.options[betDropdown.value].text);

        if (bank >= betValue)
        {
            currentBet = betValue;
            bank -= currentBet;
            UpdateBankUI();
        }
        else
        {
            // Si intenta apostar más de lo que tiene, le cobramos todo su saldo
            currentBet = bank;
            bank = 0;
            UpdateBankUI();
        }

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
                bank += currentBet;
            }
            else if (playerPoints == 21)
            {
                finalMessage.text = "¡Blackjack! Has ganado.";
                bank += currentBet * 2;
            }
            else
            {
                finalMessage.text = "El Dealer tiene Blackjack. Has perdido.";
            }

            UpdateBankUI();
            betDropdown.interactable = true;
            playAgainButton.interactable = true; // Reactivamos Play
        }
    }

    private void CalculateProbabilities()
    {
        // Obtenemos los puntos actuales del jugador
        int playerPoints = player.GetComponent<CardHand>().points;
        int remainingCards = 52 - cardIndex;

        // Evitar división por cero si se acaban las cartas (improbable en una sola mano, pero seguro)
        if (remainingCards <= 0)
        {
            probMessage.text = "Deal > Play: 0\n17<=X<=21: 0\nX > 21: 0";
            return;
        }

        int dealerWinCount = 0;
        int hit17to21 = 0;
        int hitOver21 = 0;

        // Simulamos sacar cada una de las cartas que quedan en el mazo
        for (int i = cardIndex; i < 52; i++)
        {
            int newVal = values[i];

            // --- Prob 1: ¿El dealer supera al jugador? ---
            // Tomamos los puntos reales actuales del dealer
            int dealerTotal = dealer.GetComponent<CardHand>().points;

            // Si el dealer ya tiene más puntos, cuenta como victoria para él.
            // (Esta es una simplificación de estimación similar a la del otro código)
            if (dealerTotal > playerPoints)
            {
                dealerWinCount++;
            }

            // --- Prob 2 y 3: Probabilidad del jugador pidiendo carta ---
            int newPoints = playerPoints;

            // Calcular nueva puntuación teniendo en cuenta el valor del As
            if (newVal == 11)
            {
                newPoints += (playerPoints + 11 <= 21) ? 11 : 1;
            }
            else
            {
                newPoints += newVal;
            }

            // Si se pasa de 21, comprobamos si tiene un As previo contado como 11 que pueda bajar a 1
            if (newPoints > 21 && playerPoints + newVal - 10 <= 21 && newVal == 11)
            {
                newPoints = playerPoints + 1;
            }

            // Clasificamos el resultado
            if (newPoints >= 17 && newPoints <= 21)
            {
                hit17to21++;
            }
            else if (newPoints > 21)
            {
                hitOver21++;
            }
        }

        // Calculamos los porcentajes (casos favorables / cartas restantes)
        float probDealerWin = (float)dealerWinCount / remainingCards;
        float prob17to21 = (float)hit17to21 / remainingCards;
        float probOver21 = (float)hitOver21 / remainingCards;

        probMessage.text = "Deal > Play: " + probDealerWin.ToString("F4") + "\n" +
                           "17<=X<=21: " + prob17to21.ToString("F4") + "\n" +
                           "X>21: " + probOver21.ToString("F4");
    }

    void PushDealer()
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        CalculateProbabilities();
    }

    public void Hit()
    {
        PushPlayer();

        if (player.GetComponent<CardHand>().points > 21)
        {
            dealer.GetComponent<CardHand>().InitialToggle();
            hitButton.interactable = false;
            stickButton.interactable = false;
            finalMessage.text = "¡Te has pasado de 21! Has perdido.";

            betDropdown.interactable = true;
            playAgainButton.interactable = true; // Reactivamos Play
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
            bank += currentBet * 2;
        }
        else if (dPoints > pPoints)
        {
            finalMessage.text = "El Dealer tiene más puntos. ¡Has perdido!";
        }
        else if (dPoints < pPoints)
        {
            finalMessage.text = "Tienes más puntos que el Dealer. ¡Has ganado!";
            bank += currentBet * 2;
        }
        else
        {
            finalMessage.text = "Habéis empatado.";
            bank += currentBet;
        }

        UpdateBankUI();

        betDropdown.interactable = true;
        playAgainButton.interactable = true; // Reactivamos Play
    }

    public void PlayAgain()
    {
        // Bloqueamos menú al empezar
        betDropdown.interactable = false;
        playAgainButton.interactable = false;

        hitButton.interactable = true;
        stickButton.interactable = true;
        finalMessage.text = "";

        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();
        cardIndex = 0;

        ShuffleCards();
        StartGame();
    }

    private void UpdateBankUI()
    {
        if (bankText != null)
        {
            bankText.text = "Credito: " + bank.ToString();
        }
    }
}