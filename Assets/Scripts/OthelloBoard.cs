using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class OthelloBoard : MonoBehaviour {
    public int CurrentTurn = 0;// 現在のターンを表す変数。0がプレイヤー1、1がプレイヤー2を表す。
    public GameObject ScoreBoard;// スコアボードのゲームオブジェクト
    public Text ScoreBoardText;// スコアボードに表示するテキスト
    public GameObject Template;// セルのテンプレートとなるゲームオブジェクト
    public int BoardSize = 8;// ボードのサイズ（縦横のセル数）
    public List<Color> PlayerChipColors;// プレイヤーのチップの色を格納するリスト
    public List<Vector2> DirectionList;// 方向を表すベクトルのリスト（8方向）
    static OthelloBoard instance;// シングルトンインスタンス
    public static OthelloBoard Instance { get { return instance; } }// シングルトンインスタンスのプロパティ
    OthelloCell[,] OthelloCells;// オセロのセルを格納する2次元配列
    public int EnemyID { get { return (CurrentTurn+1) % 2; } }// 敵プレイヤーのIDを取得するプロパティ
    public Button RetryButton; // 追加// リトライボタン
    void Start()// ゲーム開始時に呼ばれるメソッド
    {
        instance = this;// シングルトンインスタンスの設定
        OthelloBoardIsSquareSize();// ボードが正方形であることを確認
       
       // オセロのセルを初期化
        OthelloCells = new OthelloCell[BoardSize, BoardSize];
        float cellAnchorSize = 1.0f / BoardSize;

        // ボード上にセルを配置
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                CreateNewCell(x,y, cellAnchorSize);
            }
        }

        ScoreBoard.GetComponent<RectTransform>().SetSiblingIndex(BoardSize*BoardSize+1);// スコアボードの表示順を設定
        GameObject.Destroy(Template);// テンプレートを破棄
        InitializeGame();// ゲームを初期化
    }

    // 新しいセルを作成するメソッド
    private void CreateNewCell(int x, int y, float cellAnchorSize)
    {
        // テンプレートから新しいセルを生成
        GameObject go = GameObject.Instantiate(Template, this.transform);
        RectTransform r = go.GetComponent<RectTransform>();

        // セルの位置とサイズを設定
        r.anchorMin = new Vector2(x * cellAnchorSize, y * cellAnchorSize);
        r.anchorMax = new Vector2((x + 1) * cellAnchorSize, (y + 1) * cellAnchorSize);

        // セルの情報を設定
        OthelloCell oc = go.GetComponent<OthelloCell>();
        OthelloCells[x, y] = oc;
        oc.Location.x = x;
        oc.Location.y = y;
    }

    // ボードが正方形であることを確認するメソッド
    private void OthelloBoardIsSquareSize()
    {
        RectTransform rect = this.GetComponent<RectTransform>();

        // 画面の幅と高さを比較して正方形に調整
        if (Screen.width > Screen.height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.height);
        }
        else
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.width); 
    }

    // ゲームを初期化するメソッド
    public void InitializeGame()
    {
        ScoreBoard.gameObject.SetActive(false);// スコアボードを非表示にする

        // 全てのセルの所有者をリセット
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].OwnerID = -1;
            }
        }

        // 初期配置を設定
        OthelloCells[3, 3].OwnerID = 0;
        OthelloCells[4, 4].OwnerID = 0;
        OthelloCells[4, 3].OwnerID = 1;
        OthelloCells[3, 4].OwnerID = 1;

        UpdatePlaceableCells(); // 駒を置けるマスを更新
}
    }

    // 指定した場所にチップを置けるか確認するメソッド
    internal bool CanPlaceHere(Vector2 location)
    {
        // 既にチップが置かれている場合は置けない
        if (OthelloCells[(int)location.x, (int)location.y].OwnerID != -1)
            return false;

        // 8方向に対して確認
        for (int direction = 0; direction < DirectionList.Count; direction++)
        {
            Vector2 directionVector = DirectionList[direction];
            if (FindAllyChipOnOtherSide(directionVector, location, false) != null)
            {
                return true;
            }
        }
        return false;
    }

    // 指定したセルにチップを置くメソッド
    internal void PlaceHere(OthelloCell othelloCell)
    {
        // 8方向に対して確認し、チップをひっくり返す
        for (int direction = 0; direction < DirectionList.Count; direction++)
        {
            Vector2 directionVector = DirectionList[direction];
            OthelloCell onOtherSide = FindAllyChipOnOtherSide(directionVector, othelloCell.Location, false);
            if (onOtherSide != null)
            {
                ChangeOwnerBetween(othelloCell, onOtherSide, directionVector);
            }
        }
        OthelloCells[(int)othelloCell.Location.x, (int)othelloCell.Location.y].OwnerID = CurrentTurn;// 指定したセルに現在のターンのプレイヤーのチップを置く
    }

    // 指定した方向に味方のチップがあるか確認するメソッド
    private OthelloCell FindAllyChipOnOtherSide(Vector2 directionVector, Vector2 from, bool EnemyFound)
    {
        Vector2 to = from + directionVector;

        // ボードの範囲内かつセルが空でない場合
        if (IsInRangeOfBoard(to) && OthelloCells[(int)to.x, (int)to.y].OwnerID != -1)
        {
            // 味方のチップが見つかった場合
            if (OthelloCells[(int)to.x, (int)to.y].OwnerID == OthelloBoard.Instance.CurrentTurn)
            {
                if (EnemyFound)
                    return OthelloCells[(int)to.x, (int)to.y];
                return null;
            }
            else// 敵のチップが見つかった場合、再帰的に確認
                return FindAllyChipOnOtherSide(directionVector, to, true);
        }
        return null;
    }

    // 指定したポイントがボードの範囲内か確認するメソッド
    private bool IsInRangeOfBoard(Vector2 point)
    {
        return point.x >= 0 && point.x < BoardSize && point.y >= 0 && point.y < BoardSize;
    }

    // 指定した範囲のチップの所有者を変更するメソッド
    private void ChangeOwnerBetween(OthelloCell from, OthelloCell to, Vector2 directionVector)
    {
        for (Vector2 location = from.Location + directionVector; location != to.Location; location += directionVector)
        {
            OthelloCells[(int)location.x, (int)location.y].OwnerID = CurrentTurn;
        }
    }

    private void HighlightPlaceableCells()
{
    List<Vector2> placeableCells = GetPlaceableCells();
    foreach (Vector2 cell in placeableCells)
    {
        OthelloCells[(int)cell.x, (int)cell.y].GetComponent<Image>().color = Color.green;
    }
}

// 駒を置けるマスの色をリセットするメソッド
private void ResetCellColors()
{
    for (int y = 0; y < BoardSize; y++)
    {
        for (int x = 0; x < BoardSize; x++)
        {
            OthelloCells[x, y].GetComponent<Image>().color = Color.white;
        }
    }
}

    private void UpdatePlaceableCells()
    {
    ResetCellColors(); // セルの色をリセット
    HighlightPlaceableCells(); // 駒を置けるマスに色をつける
    }

    // ターンを終了するメソッド
    internal void EndTurn(bool isAlreadyEnded)
    {        
        CurrentTurn = EnemyID;// ターンを切り替える

        // 置ける場所があるか確認
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (CanPlaceHere(new Vector2(x, y)))
                {
                    UpdatePlaceableCells(); // 駒を置けるマスを更新
                    ResetCellColors(); // セルの色をリセット
                    HighlightPlaceableCells(); // 駒を置けるマスに色をつける
                    return;
                }
            }
        }

        // 置ける場所がない場合、ゲームオーバーを確認
        if (isAlreadyEnded)
            GameOver();
        else {
            EndTurn(true);
        }            
    }

    // ゲームオーバー時の処理
    public void GameOver()
    {
        // 全てのセルを非アクティブにする
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].GetComponent<Button>().interactable = false;
            }
        }

        // スコアを計算
        int white = CountScoreFor(0);
        int black = CountScoreFor(1);

        // 勝敗を表示
        if (white > black)
            ScoreBoardText.text = "White wins " + white + ":" + black;
        else if (black > white)
            ScoreBoardText.text = "Black wins " + black + ":" + white;
        else
            ScoreBoardText.text = "Draw! " + white + ":" + black;
        ScoreBoard.gameObject.SetActive(true);// スコアボードを表示
    }

    // リトライボタンが押された時の処理
    public void Retry()// 追加
    {
        InitializeGame();// ゲームを初期化
        ScoreBoard.gameObject.SetActive(false);// スコアボードを非表示にする

        // 全てのセルをアクティブにする
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].GetComponent<Button>().interactable = true;
            }
        }
        ResetCellColors(); // セルの色をリセット
        HighlightPlaceableCells(); // 駒を置けるマスに色をつける
    }// 追加

    // 指定したプレイヤーのスコアを計算するメソッド
    private int CountScoreFor(int owner)
    {
        int count = 0;
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (OthelloCells[x, y].OwnerID == owner) {
                    count++;
                }
            }
        }
        return count;
    }
    
}