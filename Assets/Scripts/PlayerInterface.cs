using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface PlayerInterface { //the interface the is inherited by the player objs
	int GetPlayerId();
	void turn();
	bool skipStatus {
		get;
		set;
	}
	public void DestroyCard(UnoGame.UnoGameCardInfo cardInfo);
	GameObject GetHandObject();
	public List<UnoGame.UnoGameCardInfo> GetCards();
	void addCards(UnoGame.UnoGameCardInfo other);
	string getName();
	bool Equals(PlayerInterface other);
	int getCardsLeft();
}
