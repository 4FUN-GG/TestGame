using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmbedIO.WebSockets;
using FourFun;
using FourFun.Server;
using UnityEngine;
using UnityEngine.UI;

public class UnoGame : MonoBehaviour
{
    public class UnoGameCardInfo
    {
        public int number { get; set; }
        public string color { get; set; }
    }

    public GameObject[] qrCodes;
    public GameObject[] playerHands;
    public GameObject discardPileObject;
    public GameObject deckPileObject;
    public GameObject deckPileObject2;
    private float deckToTargetDuration = 0.2f;
    public List<UnoCard> discardPile = new List<UnoCard>();
    public Dictionary<int, PlayerInterface> playerControllers = new Dictionary<int, PlayerInterface>();
    public bool[] players;

    public static UnoGame Instance;
    public GameState gameState;

    public GameObject turnIndicatorL;
    public GameObject turnIndicatorR;

    public AudioSource audioSource;
    public AudioSource audioSourceSfx;

    public int playerTurn = 0;
    public float timer = 0;
    public bool reverse = false;
    public bool hasDrawn = false;
    public bool timerStarted = true;

    private int timeForTurn = 30;

    public int drawStack = 0;

    public List<TMPro.TextMeshProUGUI> timerLabels = new List<TMPro.TextMeshProUGUI>();

    public static List<UnoGameCardInfo> deck = new List<UnoGameCardInfo>();

    public GameMode gameMode = GameMode.HouseRule;

    public enum GameState
    {
        WAITING_QR_CODE,
        PRESTART,
        START,
        END,
        PAUSED
    }

    public enum GameMode
    {
        Classic,
        HouseRule
    }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Application.runInBackground = true;
#if UNITY_EDITOR
        List<bool> list = new List<bool>
        {
            true,
            false,
            true,
            false
        };

#else
        List<bool> list = FourFunController.Instance.LoadPlayerPositions();
#endif
        players = list.ToArray();
        for (int i = 0; i < 4; i++)
        {
            if (players[i])
            {
                HumanPlayer player = playerHands[i].AddComponent<HumanPlayer>();
                player.SetupPlayer("Player" + i, i);
                playerControllers.Add(i, player);
            }
        }
        gameState = GameState.WAITING_QR_CODE;
        // Instantiate WebSocket
        LocalWebServer.Instance.AddWebSocketModule(new WebSocketGameModule("/game"));
        LocalWebServer.Instance.StartWebServer();

        StartCoroutine(GiveQRCodes(LocalWebServer.Instance.localAddress));
        FourFunController.Instance.SetUnityAsReady();
    }

    public void SetPlayerConnected(int playerId)
    {
        Debug.Log($"{playerId} connected");
        if (!players[playerId])
            return;
        DestroyImmediate(qrCodes[playerId].GetComponent<RawImage>());
        Image okImage = qrCodes[playerId].gameObject.AddComponent<Image>();
        if (okImage == null)
            return;
        okImage.sprite = Resources.Load<Sprite>("ok_icon");
        okImage.preserveAspect = true;
        Destroy(qrCodes[playerId].transform.GetChild(0).gameObject);
        var allConnected = true;
        for (int i = 0; i < 4; i++)
        {
            if (players[i])
            {
                RawImage image = qrCodes[i].GetComponent<RawImage>();
                if(image != null)
                    allConnected = false;
            }
        }
        if (allConnected)
            StartCoroutine(StartUnoGame());
    }

    public IEnumerator StartUnoGame()
    {
        FourFunController.Instance.ResetIdleTime();
        gameState = GameState.PRESTART;
        HideQRCodes();
        SetupDeck();

        // Give Cards with smooth transition
        foreach (KeyValuePair<int, PlayerInterface> x in playerControllers)
        {
            for (int i = 0; i < 7; i++)
            {
                yield return StartCoroutine(Draw(1, x.Value, true));
            }
        }

        // Lay First Card
        UnoGameCardInfo first = null;
        if (deck[0].number < 10)
        {
            first = deck[0];
        }
        else
        {
            while (deck[0].number >= 10)
            {
                deck.Add(deck[0]);
                deck.RemoveAt(0);
            }
            first = deck[0];
        }
        yield return StartCoroutine(DiscardCard(first, null));
        deck.RemoveAt(0);
        gameState = GameState.START;
        deckPileObject.GetComponent<Button>().onClick.RemoveAllListeners();
        deckPileObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            StartCoroutine(DrawFromDeck());
        });
        deckPileObject2.GetComponent<Button>().onClick.RemoveAllListeners();
        deckPileObject2.GetComponent<Button>().onClick.AddListener(() =>
        {
            StartCoroutine(DrawFromDeck());
        });
        yield return null;
    }

    public IEnumerator DrawFromDeck()
    {
        if (hasDrawn)
            yield return null;
        hasDrawn = true;
        yield return Draw(1, playerControllers[playerTurn]);

        if (!CanPlay(playerControllers[playerTurn]))
        {
            timer = timeForTurn;
        }
        else
        {
            timer = 0;
        }
    }

    private void Update()
    {
        if (gameState != GameState.START)
            return;
        hasWon();
        timer += Time.deltaTime;
        if(Math.Round(timeForTurn - timer) < 5)
        {
            if (!timerStarted)
            {
                timerStarted = true;
                PlayTimer();
            }
        } else
        {
            timerStarted = false;
            PauseTimer();
        }
        if (timer < timeForTurn)
        {
            timerLabels[playerTurn].gameObject.SetActive(true);
            timerLabels[playerTurn].text = Math.Round(timeForTurn - timer, 0).ToString() + "s";
            return;
        }

        timer = 0;
        timerStarted = false;

        // Get the list of player IDs (keys) and sort them to maintain turn order
        List<int> playerKeys = playerControllers.Keys.ToList();
        playerKeys.Sort();

        // Find the current player's index in the sorted playerKeys list
        int currentIndex = playerKeys.IndexOf(playerTurn);

        // Calculate the next player's index based on the game direction
        int nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;

        // Handle wrap-around: if nextIndex is out of bounds, reset to start or end
        if (nextIndex >= playerKeys.Count)
            nextIndex = 0;  // Wrap to the first player
        else if (nextIndex < 0)
            nextIndex = playerKeys.Last();  // Wrap to the last player

        // Check if the current player has to skip their turn
        if (playerControllers[nextIndex].skipStatus)
        {
            // Reset skipTurn to false after skipping
            playerControllers[nextIndex].skipStatus = false;
            timer = timeForTurn;
        }


        // Update the playerTurn to the next player ID
        timerLabels[playerTurn].gameObject.SetActive(false);
        playerTurn = playerKeys[nextIndex];
        timerLabels[playerTurn].gameObject.SetActive(true);

        Debug.Log("Now it's player: " + playerControllers[playerTurn].getName());
        hasDrawn = false;
        if(drawStack > 0)
        {
            WebSocketGameModule.Instance.Send(playerTurn, "SetDraw", discardPile.Last().cardInfo.number.ToString());
        }
        playerControllers[playerTurn].turn();
    }

    public int GetNextPlayer()
    {
        // Get the list of player IDs (keys) and sort them to maintain turn order
        List<int> playerKeys = playerControllers.Keys.ToList();
        playerKeys.Sort();

        // Find the current player's index in the sorted playerKeys list
        int currentIndex = playerKeys.IndexOf(playerTurn);

        // Calculate the next player's index based on the game direction
        int nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;

        // Handle wrap-around: if nextIndex is out of bounds, reset to start or end
        if (nextIndex >= playerKeys.Count)
            nextIndex = 0;  // Wrap to the first player
        else if (nextIndex < 0)
            nextIndex = playerKeys.Count - 1;  // Wrap to the last player

        return playerKeys[nextIndex];
    }

    public void PlayCardAction(UnoGameCardInfo info, int playerId)
    {
        StartCoroutine(PlayCard(info, playerId));
    }

    public IEnumerator PlayCard(UnoGameCardInfo info, int playerId)
    {
        FourFunController.Instance.ResetIdleTime();
        hasDrawn = false;
        yield return StartCoroutine(DiscardCard(info, playerHands[playerId].transform));
        playerControllers[playerId].DestroyCard(info);
        Destroy(playerHands[playerId].transform.GetChild(0).gameObject);
        playerControllers[playerId].GetHandObject().GetComponent<CardHandLayout>().UpdateLayout();
        /*
	    * 1-9 are regular
	    * 10 is skip
	    * 11 is reverse
	    * 12 is draw 2
	    * 13 is wild
	    * 14 is wild draw 4
	    */
        PlayAudioOneShot("Sound/Card/SFX_Card_Select");
        if (info.number == 10)
        {
            PlayAudioOneShot("Sound/Male/DIA_F_06_Skip");
            playerControllers[GetNextPlayer()].skipStatus = true;
            timer = timeForTurn;
        }
        else if (info.number == 11)
        {
            reverse = !reverse;
            PlayAudioOneShot("Sound/Male/DIA_M_05_Revers");
            UpdatePlayerTurnIndicator();
            timer = timeForTurn;
        }
        else if (info.number == 12)
        {
            PlayAudioOneShot("Sound/Male/DIA_M_07_Draw2");
            var nextPlayer = playerControllers[GetNextPlayer()];
            drawStack += 2;
            if (nextPlayer.GetCards().Find(x => x.number == 12 || x.number == 14) == null)
            {
                StartCoroutine(Draw(drawStack, playerControllers[GetNextPlayer()]));
            }
            timer = timeForTurn;
        }
        else if (info.number == 13)
        {
            PlaySoundChoosenColor(info.color);
            timer = timeForTurn;
        }
        else if (info.number == 14)
        {
            PlaySoundChoosenColor(info.color);
            PlayAudioOneShot("Sound/Male/DIA_M_08_Draw4");
            var nextPlayer = playerControllers[GetNextPlayer()];
            drawStack += 4;
            if (nextPlayer.GetCards().Find(x => x.number == 14) == null)
            {
                StartCoroutine(Draw(drawStack, playerControllers[GetNextPlayer()]));
            }
            timer = timeForTurn;
        }
        if (info.number < 10)
        {
            timer = timeForTurn;
        }
    }

   

    public void UpdatePlayerTurnIndicator()
    {
        if (!reverse)
        {
            turnIndicatorL.transform.localScale = Vector3.one;
            turnIndicatorR.transform.localScale = Vector3.one;
        }
        else
        {
            turnIndicatorL.transform.localScale = new Vector3(1, -1, 1);
            turnIndicatorR.transform.localScale = new Vector3(1, -1, 1);
        }
    }

    bool hasWon()
    {
        foreach (KeyValuePair<int, PlayerInterface> i in playerControllers)
        {
            if (i.Value.getCardsLeft() == 0)
            {
                DestroyImmediate(playerHands[i.Key].transform.GetChild(0));
                Instantiate(Resources.Load<GameObject>("Prefabs/Won"), playerHands[i.Key].transform);
                gameState = GameState.END;
                StartCoroutine(ExitOnWin());
                return true;
            }
        }
        return false;
    }

    public IEnumerator ExitOnWin()
    {
        yield return new WaitForSeconds(10);
        Application.Quit();
    }
    bool CanPlay(PlayerInterface player)
    {
        UnoGameCardInfo currentCard = discardPile.Last().cardInfo;
        List<UnoGameCardInfo> handCards = player.GetCards();

        foreach (UnoGameCardInfo card in handCards)
        {
            // Check if the card is playable based on wild card, matching color, or matching number
            if (card.number == 13 || card.number == 14 ||   // Wild cards
                card.color == currentCard.color ||           // Matching color
                card.number == currentCard.number)           // Matching number
            {
                return true; // The player has a playable card
            }
        }

        return false;
    }

    public IEnumerator Draw(int amount, PlayerInterface who, bool playDealSound = false)
    {
        drawStack = 0;
        if (deck.Count < amount)
        {
            ResetDeck();
        }
        for (int i = 0; i < amount; i++)
        {
            Debug.Log("Drawing Card");
            // Instantiate card at the deck pile position
            var deckPile = deckPileObject.transform;
            if (who.GetPlayerId() == 0 || who.GetPlayerId() == 3)
            {
                deckPile = deckPileObject2.transform;
            }
            GameObject newCard = Instantiate(Resources.Load("Prefabs/CardBack"), deckPile.position, deckPile.rotation) as GameObject;
            newCard.transform.SetParent(who.GetHandObject().transform);

            // Smoothly move the card from deck to player's hand
            Transform playerHandTransform = who.GetHandObject().transform;
            if (playDealSound)
            {
                PlayAudioOneShot("Sound/Card/SFX_Card_Deal_Comm");
            }
            else
            {
                PlayAudioOneShot("Sound/Card/SFX_Card_Draw_Comm");
            }
            yield return StartCoroutine(SmoothMoveCard(newCard, playerHandTransform, deckToTargetDuration));
            who.GetHandObject().GetComponent<CardHandLayout>().UpdateLayout();

            // Add card to player's hand logic after the transition completes
            who.addCards(deck[0]);
            deck.RemoveAt(0);
            WebSocketGameModule.Instance.Send(who.GetPlayerId(), "GetHand", Newtonsoft.Json.JsonConvert.SerializeObject(who.GetCards()));
        }
    }

    public void ResetDeck()
    {
        foreach (UnoCard x in discardPile)
        {
            if (x.cardInfo.number == 13 || x.cardInfo.number == 14)
            {
                x.cardInfo.color = "Black";
            }
            deck.Add(x.cardInfo);
        }
        Shuffle();
        UnoCard lastCard = discardPile[discardPile.Count - 1];
        UnoGameCardInfo last = lastCard.cardInfo;
        StartCoroutine(DiscardCard(last, null));
        discardPile.Clear();
        discardPile.Add(lastCard);
    }

    public IEnumerator DiscardCard(UnoGameCardInfo card, Transform origin)
    {
        if (origin == null)
            origin = deckPileObject.transform;
        /*
	    * 1-9 are regular
	    * 10 is skip
	    * 11 is reverse
	    * 12 is draw 2
	    * 13 is wild
	    * 14 is wild draw 4
	    */
        GameObject newCard;
        switch (card.number)
        {
            case 10:
                newCard = Instantiate(Resources.Load("Prefabs/Skip"), origin.position, origin.rotation) as GameObject;
                break;
            case 11:
                newCard = Instantiate(Resources.Load("Prefabs/Reverse"), origin.position, origin.rotation) as GameObject;
                break;
            case 12:
                newCard = Instantiate(Resources.Load("Prefabs/DrawTwo"), origin.position, origin.rotation) as GameObject;
                break;
            case 13:
                newCard = Instantiate(Resources.Load("Prefabs/Wild"), origin.position, origin.rotation) as GameObject;
                break;
            case 14:
                newCard = Instantiate(Resources.Load("Prefabs/DrawFour"), origin.position, origin.rotation) as GameObject;
                break;
            default:
                newCard = Instantiate(Resources.Load("Prefabs/Card"), origin.position, origin.rotation) as GameObject;
                break;
        }
        newCard.transform.SetParent(discardPileObject.transform);
        UnoCard unoCard = newCard.AddComponent<UnoCard>();
        unoCard.SetCardInfo(card);
        yield return StartCoroutine(SmoothMoveCard(newCard, discardPileObject.transform, deckToTargetDuration));
        discardPile.Add(unoCard);
        foreach(KeyValuePair<int, IWebSocketContext> player in WebSocketGameModule.Instance.clients)
        {
            WebSocketGameModule.Instance.Send(player.Key, "GetCurrentCard", Newtonsoft.Json.JsonConvert.SerializeObject(discardPile.Last().cardInfo));
        }
    }

    public void HideQRCodes()
    {
        for (int i = 0; i < 4; i++)
        {
            qrCodes[i].SetActive(false);
        }
    }

    public void SetupDeck()
    {
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                switch (i)
                {
                    case 13:
                    case 14:
                        deck.Add(new UnoGameCardInfo { number = i, color = "Black" });
                        break;
                    default:
                        deck.Add(new UnoGameCardInfo { number = i, color = ReturnColorName(j % 4) });
                        break;
                }

                if ((i == 0 || i >= 13) && j >= 3)
                    break;
            }
        }
        Shuffle();
    }

    void Shuffle()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            UnoGameCardInfo temp = deck.ElementAt(i);
            int posSwitch = UnityEngine.Random.Range(0, deck.Count);
            deck[i] = deck[posSwitch];
            deck[posSwitch] = temp;
        }
    }

    public IEnumerator GiveQRCodes(string url)
    {
        for (int i = 0; i < 4; i++)
        {
            if (players[i] != false)
            {
                if (playerHands[i].transform.childCount != 0)
                    Destroy(playerHands[i].transform.GetChild(0).gameObject);
                qrCodes[i].GetComponent<QRCodeDisplay>().GenerateQRCode(url, i);
            }
            else
            {
                qrCodes[i].SetActive(false);
            }
        }
        yield return null;
    }

    public IEnumerator SmoothMoveCard(GameObject card, Transform target, float duration)
    {
        Vector3 startPosition = card.transform.position;
        Quaternion startRotation = card.transform.rotation;
        Vector3 startScale = card.transform.localScale;

        Vector3 endPosition = target.position;
        Quaternion endRotation = target.rotation;
        Vector3 endScale = target.localScale; // Optional if you want to scale the card

        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            // Smoothly interpolate position, rotation, and scale
            card.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            card.transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);
            card.transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / duration); // Optional

            elapsedTime += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure final position, rotation, and scale are set
        card.transform.position = endPosition;
        card.transform.localScale = endScale; // Optional
    }

    string ReturnColorName(int number)
    {
        switch (number)
        {
            case 0:
                return "Green";
            case 1:
                return "Blue";
            case 2:
                return "Red";
            case 3:
                return "Yellow";
        }
        return "Black";
    }

    void PlaySoundChoosenColor(string color)
    {
        switch (color)
        {
            case "Yellow":
                PlayAudioOneShot("Sound/Male/DIA_M_03_Ylow");
                break;
            case "Red":
                PlayAudioOneShot("Sound/Male/DIA_M_02_Red");
                break;
            case "Blue":
                PlayAudioOneShot("Sound/Male/DIA_M_04_Blue");
                break;
            case "Green":
                PlayAudioOneShot("Sound/Male/DIA_M_01_Green");
                break;
        }
    }

    void PlayTimer()
    {
        audioSourceSfx.clip = Resources.Load<AudioClip>("Sound/SFX_Play_Hurry_5s");
        audioSourceSfx.loop = false;
        audioSourceSfx.Play();
    }

    void PauseTimer()
    {
        audioSourceSfx.Pause();
        audioSourceSfx.clip = null;
    }

    void PlayAudioOneShot(string resPath)
    {
        if (audioSource == null)
            return;

        AudioClip audioClip = Resources.Load<AudioClip>(resPath); 
        if(audioClip == null) return;
        audioSource.PlayOneShot(audioClip);
    }
}
