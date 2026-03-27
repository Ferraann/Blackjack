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
        for (int i = 51; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
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
        // --- MÉTODO DE TU COMPAÑERO PARA LAS APUESTAS ---
        // (Evita errores si tienes puesto "10 Credits" en vez de solo "10")
        currentBet = GetBetFromDropdown();

        if (bank >= currentBet)
        {
            bank -= currentBet;
            UpdateBankUI();
        }
        else
        {
            currentBet = bank;
            bank = 0;
            UpdateBankUI();
        }

        // Repartimos usando exactamente el mismo orden de tu compañero
        PushPlayer();
        PushDealer();
        PushPlayer();
        PushDealer();

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
            playAgainButton.interactable = true;
        }
    }

    // ─────────────────────────────────────────────
    // MÉTODO EXACTO DE TU COMPAÑERO PARA LA PROBABILIDAD
    // ─────────────────────────────────────────────
    private void CalculateProbabilities()
    {
        int playerPoints = player.GetComponent<CardHand>().points;
        int remainingCards = 52 - cardIndex;

        if (remainingCards <= 0)
        {
            if (probMessage != null)
                probMessage.text = "Probabilidades:\nDeal > Play: -\n17<=X<=21: -\nX > 21: -";
            return;
        }

        // --- Prob 1: Con la carta oculta, ¿el dealer supera al jugador? ---
        int dealerWinCount = 0;
        for (int i = cardIndex; i < 52; i++)
        {
            int hiddenVal = values[i];
            int dealerTotal = dealer.GetComponent<CardHand>().points;

            if (dealerTotal > playerPoints)
                dealerWinCount++;
        }
        float probDealerWin = (float)dealerWinCount / remainingCards;

        // --- Prob 2: Probabilidad de que el jugador obtenga 17-21 pidiendo una carta ---
        int hit17to21 = 0;
        int hitOver21 = 0;

        for (int i = cardIndex; i < 52; i++)
        {
            int newVal = values[i];
            int newPoints = playerPoints;

            if (newVal == 11)
                newPoints += (playerPoints + 11 <= 21) ? 11 : 1;
            else
                newPoints += newVal;

            if (newPoints > 21 && playerPoints + newVal - 10 <= 21 && newVal == 11)
                newPoints = playerPoints + 1;

            if (newPoints >= 17 && newPoints <= 21)
                hit17to21++;
            else if (newPoints > 21)
                hitOver21++;
        }

        float prob17to21 = (float)hit17to21 / remainingCards;
        float probOver21 = (float)hitOver21 / remainingCards;

        if (probMessage != null)
        {
            probMessage.text = "Probabilidades:\n" +
                               "Deal > Play: " + probDealerWin.ToString("F4") + "\n" +
                               "17<=X<=21: " + prob17to21.ToString("F4") + "\n" +
                               "X>21: " + probOver21.ToString("F4");
        }
    }
    // ─────────────────────────────────────────────

    void PushDealer()
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;

        // Lo ponemos de vuelta aquí adentro, tal y como lo tiene él
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
            playAgainButton.interactable = true;
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
        playAgainButton.interactable = true;
    }

    public void PlayAgain()
    {
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

    // Usamos el helper de tu compañero para leer el desplegable sin crasheos
    private int GetBetFromDropdown()
    {
        if (betDropdown == null) return 10;
        return (betDropdown.value + 1) * 10;
    }
}