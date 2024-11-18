using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HumanPlayer : MonoBehaviour, PlayerInterface {
	int playerId;
	bool skip=false;
	bool drew =false;
	bool playedWild;
	string name;
	List<UnoGame.UnoGameCardInfo> handList = new List<UnoGame.UnoGameCardInfo> ();


	public void SetupPlayer(string name, int id)
	{
		this.name = name; 
		playerId = id;
	}

	public bool skipStatus { //returns if the player should be skipped
		get{return skip; }
		set{ skip = value; }
	}

	public void turn() { //does the turn
		playedWild = false;
		drew = false;
		int i = 0;
		foreach (UnoGame.UnoGameCardInfo x in handList) { //foreach card in hand
			
			/*GameObject temp = null;
			if (GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform.childCount > i) //is the card already there or does it need to be loaded
				temp = GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform.GetChild (i).gameObject;			
			else 
				temp = x.loadCard (GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform);

			
			if (handList [i].Equals (Control.discard [Control.discard.Count - 1]) || handList [i].getNumb () >= 13) { //if the cards can be played
				setListeners (i, temp);
			}
			else {
				temp.transform.GetChild (3).gameObject.SetActive (true); //otherwise black them out
			}
			i++;*/
		}
	}
	public void setListeners(int where,GameObject temp) { //sets all listeners on the cards
		temp.GetComponent<Button> ().onClick.AddListener (() => {
			playedWild = handList[where].number>=13;

			temp.GetComponent<Button>().onClick.RemoveAllListeners();
			Destroy (temp);
			turnEnd(where);
		});
	}
	public int GetPlayerId()
	{
		return playerId;
	}
	public GameObject GetHandObject()
	{
		return gameObject;
	}

    public void addCards(UnoGame.UnoGameCardInfo other) { //recieves cards to add to the hand
		handList.Add (other);
	}
	public void DestroyCard(UnoGame.UnoGameCardInfo card)
    {
		var tempColor = card.color;
		if(card.number == 13 || card.number == 14)
		{
            tempColor = "Black";
		}
        List<UnoGame.UnoGameCardInfo> cardsToRemove = new List<UnoGame.UnoGameCardInfo>();
        foreach (UnoGame.UnoGameCardInfo handCard in handList)
		{
			if(handCard.number == card.number && handCard.color == tempColor)
            {
                cardsToRemove.Add(handCard); // Mark this card for removal
            }
        }
        foreach (UnoGame.UnoGameCardInfo handCard in cardsToRemove)
        {
            handList.Remove(handCard);
        }
        WebSocketGameModule.Instance.Send(GetPlayerId(), "GetHand", Newtonsoft.Json.JsonConvert.SerializeObject(GetCards()));
    }

    public void recieveDrawOnTurn() { //if the player decides to draw
		/*handList[handList.Count-1].loadCard (GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform);
		drew = true;
		turnEnd (-1);*/
	}

	public List<UnoGame.UnoGameCardInfo> GetCards()
	{
		return handList;
	}
	public void turnEnd(int where) { 
	}

	public bool Equals(PlayerInterface other) { //equals function based on name
		return other.getName ().Equals (name);
	}
	public string getName() { //returns the name
		return name;
	}
	public int getCardsLeft() { //gets how many cards are left in the hand
		return handList.Count;
	}
}
