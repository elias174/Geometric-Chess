﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum PlayerType {
		P1, P2
}

public class GCPlayer : IClicker, IInputReceiver {

	private PlayerType type;

	private List<Piece> pieces;
	private List<Piece> eatenPieces;

	private Piece piece;
	private Piece checkedBy; //Experimental

	//Experimental
	public bool IsChecked {
		get {return checkedBy != null;}
	}

	public List<Piece> Pieces {
		get {return pieces;}
	}

	public List<Piece> EatenPieces {
		get {return eatenPieces;}
	}

	//Experimental
	public Piece CheckedBy {
		get {return checkedBy;}
		set {
			checkedBy = value;
		}
	}

	public Piece HoldingPiece {
		get {return piece;}
	}

	public bool IsReady {
		get {
			for (int i = 0; i < pieces.Count; i++) {
				if (!pieces[i].IsReady) return false;
			}

			return true;
		}
	}

	public PlayerType Type {
		get {return type;}
	}

	public GCPlayer(PlayerType type) {
		this.type = type;
		pieces = new List<Piece>();
		eatenPieces = new List<Piece>();
	}

	public void EnableInput() {
		InputManager.InputEvent += OnInputEvent;
	}

	public void DisableInput() {
		InputManager.InputEvent -= OnInputEvent;
	}

	void OnDisable() {
		DisableInput();
	}

	public void OnInputEvent(InputActionType action) {
		switch (action) {
			case InputActionType.GRAB_PIECE:
                if (GameManager.Instance.temporal_move != null) break;
				Node gNode = Finder.RayHitFromScreen<Node>(Input.mousePosition);
				if (gNode == null) break;
				piece = gNode.Piece;
				if (piece == null) break;
				if (!piece.IsReady) break;
				if (Click(gNode) && piece && Has(piece) && Click(piece)) {
					piece.Pickup();
					piece.Compute();
					piece.HighlightPossibleMoves();
					piece.HighlightPossibleEats();
                    GameManager.Instance.temporal_source_position = piece.ChessCoords;
					GameManager.Instance.GameState.Grab();
				} 

				//check clickable for tile and piece then pass Player
				//check if player has piece - PIECE 
				//check if player has piece if not empty - NODE 
				break;
			case InputActionType.CANCEL_PIECE:
					if (piece != null) {
						//if (!piece.IsReady) break;
						piece.Drop();
						piece = null;
						GameManager.Instance.GameState.Cancel();
					}
				break;
			case InputActionType.PLACE_PIECE:
                // Event afer select a target position
				Node tNode = Finder.RayHitFromScreen<Node>(Input.mousePosition);
				if (tNode == null) break;
				Piece tPiece = tNode.Piece;
                GameManager.Instance.temporal_target_position = tNode.ChessCoords;
                if (tPiece == null) {
					if (piece.IsPossibleMove(tNode)) {
						if (Rules.IsCheckMove(this,piece,tNode, true)) {
							Debug.Log("Move checked"); // do nothing
						} else {
							piece.MoveToXZ(tNode, Drop);
							GameManager.Instance.GameState.Place();
						}
					}
				} else {
					if (piece.IsPossibleEat(tNode)) {
						if (Rules.IsCheckEat(this,piece,tNode, true)) {
							Debug.Log("Eat checked"); // do nothing
						} else {
							GCPlayer oppPlayer = GameManager.Instance.Opponent(this);
							oppPlayer.RemovePiece(tPiece);
							AddEatenPieces(tPiece);
							tPiece.ScaleOut(0.2f, 1.5f);
							piece.MoveToXZ(tNode, Drop);
							GameManager.Instance.GameState.Place();
						}
					}
				}
                if (GameManager.Instance.temporal_move == null) {
                    GameManager.Instance.temporal_move = Tuple.Create<string, string>(
                        GameManager.Instance.temporal_source_position, GameManager.Instance.temporal_target_position);
                }
                break;
		}
	}

    public void simulate_move(Node node_from, Node node_target, bool quick_move = false)
    {
        // First select action move
        piece = node_from.Piece;
        Piece tPiece = node_target.Piece;
        piece.set_speed(200f);
        if (Click(node_from) && piece && Has(piece) && Click(piece))
        {
            piece.Pickup();
            piece.Compute();
            piece.HighlightPossibleMoves();
            piece.HighlightPossibleEats();
            GameManager.Instance.GameState.Grab();
        }

        // Move action process
        piece.set_speed(200f);
        if (tPiece == null)
        {
            if (piece.IsPossibleMove(node_target))
            {
                if (Rules.IsCheckMove(this, piece, node_target, true))
                {
                    Debug.Log("Move checked"); // do nothing
                }
                else
                {
                    piece.MoveToXZ(node_target, Drop);
                    GameManager.Instance.GameState.Place();
                }
            }
        }
        else
        {
            if (piece.IsPossibleEat(node_target))
            {
                if (Rules.IsCheckEat(this, piece, node_target, true))
                {
                    Debug.Log("Eat checked"); // do nothing
                }
                else
                {
                    GCPlayer oppPlayer = GameManager.Instance.Opponent(this);
                    oppPlayer.RemovePiece(tPiece);
                    AddEatenPieces(tPiece);
                    // tPiece.ScaleOut(0.2f, 1.5f);
                    piece.MoveToXZ(node_target, Drop);
                    GameManager.Instance.GameState.Place();
                }
            }
        }
        piece.set_speed(5f);
    }

    public void ClearPiecesPossibles() {
		for (int i = 0; i < pieces.Count; i++) {
			pieces[i].ClearPossibleEats();
			pieces[i].ClearPossibleMoves();
		}
	}

	public void ClearCheck() {
		if (checkedBy == null) return;
		checkedBy = null;
		//checkedBy.ClearCheck(this);
	}

	//the methods inside must be in order
	private void Drop() {
		piece.Drop();
		piece.Compute();
        GameManager.Instance.GameState.Release();
		piece = null;
	}

	public bool Has(Piece piece) {
		return pieces.Contains(piece);
	}

	public bool Click(IClickable clickable) {
		if (clickable == null) return false;
		return clickable.Inform<GCPlayer>(this); 
	}

	public void AddPieces(params Piece[] pieces) {
		for (int i = 0; i < pieces.Length; i++) {
			this.pieces.Add(pieces[i]);
		}
	}

	public void AddEatenPieces(params Piece[] pieces) {
		for (int i = 0; i < pieces.Length; i++) {
			this.eatenPieces.Add(pieces[i]);
		}
	}

	public bool RemovePiece(Piece piece) {
		return pieces.Remove(piece);
	}

	public void ComputePieces() {
		for (int i = 0; i < pieces.Count; i++) {
			pieces[i].Compute();
		}
	}
}
