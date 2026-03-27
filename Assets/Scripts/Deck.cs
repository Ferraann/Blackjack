using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Deck : MonoBehaviour
{
    // --- VARIABLES DE INTERFAZ Y OBJETOS ---
    public Sprite[] faces;
    public GameObject dealer;
    public GameObject player;
    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Text finalMessage;
    public Text probMessage;

    // --- VARIABLES DE LA BANCA ---
    public Text bankText;
    public TMP_Dropdown betDropdown;
    private int bank = 1000;
    private int currentBet = 0;

    // --- VARIABLES DEL MAZO ---
    public int[] values = new int[52];
    int cardIndex = 0;

    private void Awake()
    {
        // Prepara los valores de las cartas nada más cargar el juego
        InitCardValues();
    }

    private void Start()
    {
        // ESTADO INICIAL: Bloqueamos los botones de juego para obligar al jugador 
        // a elegir su apuesta y pulsar "Play" antes de repartir cartas.
        hitButton.interactable = false;
        stickButton.interactable = false;
        finalMessage.text = "Elige apuesta y pulsa Play";
        UpdateBankUI();
    }

    // Función que asigna los puntos correspondientes a cada carta de la baraja
    private void InitCardValues()
    {
        for (int i = 0; i < 52; i++)
        {
            // El módulo 13 nos da la posición de la carta dentro de su palo (0 a 12)
            int cardRank = i % 13;
            if (cardRank == 0) values[i] = 11; // Posición 0 es el As
            else if (cardRank >= 1 && cardRank <= 9) values[i] = cardRank + 1; // Cartas numéricas (2-10)
            else values[i] = 10; // Figuras (J, Q, K)
        }
    }

    // Función que mezcla la baraja usando el algoritmo Fisher-Yates
    private void ShuffleCards()
    {
        for (int i = 51; i > 0; i--)
        {
            // Elige una posición al azar e intercambia tanto la imagen como el valor
            int randomIndex = Random.Range(0, i + 1);

            Sprite tempFace = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempFace;

            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;
        }
    }

    // Función que arranca la ronda al pulsar Play
    void StartGame()
    {
        // 1. GESTIÓN DE APUESTA: Cobramos el dinero antes de jugar
        currentBet = GetBetFromDropdown();

        if (bank >= currentBet)
        {
            bank -= currentBet;
            UpdateBankUI();
        }
        else
        {
            // Si intenta apostar más de lo que tiene, le apostamos lo que le queda
            currentBet = bank;
            bank = 0;
            UpdateBankUI();
        }

        // 2. REPARTO INICIAL: 2 cartas alternas para cada uno
        PushPlayer();
        PushDealer();
        PushPlayer();
        PushDealer();

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        // 3. COMPROBACIÓN DE BLACKJACK: Si alguien saca 21 con las 2 primeras cartas, la ronda acaba
        if (playerPoints == 21 || dealerPoints == 21)
        {
            dealer.GetComponent<CardHand>().InitialToggle();
            hitButton.interactable = false;
            stickButton.interactable = false;

            if (playerPoints == 21 && dealerPoints == 21)
            {
                finalMessage.text = "¡Empate a Blackjack!";
                bank += currentBet; // Se devuelve la apuesta
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
            betDropdown.interactable = true;
            playAgainButton.interactable = true;
        }
    }

    // Función matemática que calcula las probabilidades basándose en las cartas que quedan sin repartir
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

        int dealerWinCount = 0;
        int hit17to21 = 0;
        int hitOver21 = 0;

        // Simulamos qué pasaría si saliera cada una de las cartas que quedan en el mazo
        for (int i = cardIndex; i < 52; i++)
        {
            int newVal = values[i];

            // ESTIMACIÓN DEL DEALER: ¿Gana el dealer con sus puntos actuales?
            int dealerTotal = dealer.GetComponent<CardHand>().points;
            if (dealerTotal > playerPoints)
                dealerWinCount++;

            // ESTIMACIÓN DEL JUGADOR: ¿Qué pasa si el jugador pide una carta más?
            int newPoints = playerPoints;

            // Lógica para que el As valga 1 u 11 sin pasarnos de 21
            if (newVal == 11)
                newPoints += (playerPoints + 11 <= 21) ? 11 : 1;
            else
                newPoints += newVal;

            if (newPoints > 21 && playerPoints + newVal - 10 <= 21 && newVal == 11)
                newPoints = playerPoints + 1;

            // Contabilizamos si la carta nos favorece (17-21) o nos hace perder (>21)
            if (newPoints >= 17 && newPoints <= 21)
                hit17to21++;
            else if (newPoints > 21)
                hitOver21++;
        }

        // Calculamos los porcentajes finales y los mostramos en el formato requerido
        float probDealerWin = (float)dealerWinCount / remainingCards;
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

    // Reparte una carta al Dealer
    void PushDealer()
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    // Reparte una carta al Jugador y recalcula las probabilidades al instante
    void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;

        CalculateProbabilities();
    }

    // Función ejecutada por el botón HIT (Pedir carta)
    public void Hit()
    {
        PushPlayer();

        // REGLA DE DERROTA: Si al pedir te pasas de 21, pierdes inmediatamente
        if (player.GetComponent<CardHand>().points > 21)
        {
            dealer.GetComponent<CardHand>().InitialToggle(); // Volteamos la carta del dealer por cortesía
            hitButton.interactable = false;
            stickButton.interactable = false;
            finalMessage.text = "¡Te has pasado de 21! Has perdido.";

            betDropdown.interactable = true;
            playAgainButton.interactable = true;
        }
    }

    // Función ejecutada por el botón STAND (Plantarse)
    public void Stand()
    {
        // 1. Termina el turno del jugador y se revela la carta oculta del crupier
        dealer.GetComponent<CardHand>().InitialToggle();
        hitButton.interactable = false;
        stickButton.interactable = false;

        CardHand dealerHand = dealer.GetComponent<CardHand>();
        CardHand playerHand = player.GetComponent<CardHand>();

        // 2. IA DEL CRUPIER: Por reglas del casino, está obligado a pedir mientras tenga 16 o menos
        while (dealerHand.points <= 16)
        {
            PushDealer();
        }

        // 3. RESOLUCIÓN: Comparamos puntos finales y repartimos el dinero
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

    // Función para reiniciar la mesa y jugar otra ronda
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

    // Actualiza el texto en pantalla de la banca
    private void UpdateBankUI()
    {
        if (bankText != null)
        {
            bankText.text = "Credito: " + bank.ToString();
        }
    }

    // Transforma la posición del desplegable en euros (Posición 0 = 10, Posición 1 = 20...)
    private int GetBetFromDropdown()
    {
        if (betDropdown == null) return 10;
        return (betDropdown.value + 1) * 10;
    }
}