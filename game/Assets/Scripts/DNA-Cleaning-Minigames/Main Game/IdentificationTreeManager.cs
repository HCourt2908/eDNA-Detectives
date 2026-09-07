using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdentificationTreeManager : MonoBehaviour
{

    public GameObject treeNodePrefab;
    public GameObject treeConnectionPrefab;
    public Transform treeContent;

    private List<string> identificationPath = new List<string>();
    private List<GameObject> nodeObjects = new List<GameObject>();

    private int currentNode = 0;

    public float minimumSpacing = 20f;
    public float topPadding = 20f;
    public float bottomPadding = 20f;

    public bool isRevealing;

    void Start()
    {
        identificationPath.Add("Animalia");
        identificationPath.Add("Chordata");
        identificationPath.Add("Fish");
        identificationPath.Add("Sharks");
        identificationPath.Add("Great White Shark");

        isRevealing = false;
    }

    public void RevealNext()
    {
        if (currentNode >= identificationPath.Count || isRevealing) return;

        isRevealing = true;
        StartCoroutine(RevealNode());
    }

    IEnumerator RevealNode()
    {

        GameObject nodeObject = Instantiate(treeNodePrefab, treeContent);
        PositionNode(nodeObject);

        TreeNode node = nodeObject.GetComponent<TreeNode>();

        node.SetText(identificationPath[currentNode]);

        CanvasGroup canvasGroup = nodeObject.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0;

        nodeObjects.Add(nodeObject);

        if (currentNode > 0)
        {
            yield return StartCoroutine(CreateConnection());
        }

        float duration = 0.5f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            canvasGroup.alpha = time / duration;

            yield return null;
        }

        canvasGroup.alpha = 1f;

        currentNode++;

        isRevealing = false;
    }

    void PositionNode(GameObject nodeObject)
    {
        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();

        float contentHeight = treeContent.GetComponent<RectTransform>().rect.height;
        float nodeHeight = nodeRect.rect.height;

        int totalNodes = identificationPath.Count;
        float usableHeight = contentHeight - topPadding - bottomPadding - (nodeHeight * totalNodes);

        float spacing = Mathf.Max(usableHeight / (totalNodes - 1), minimumSpacing);

        float yPosition = -topPadding - (nodeHeight / 2f) - currentNode * (nodeHeight + spacing);

        nodeRect.anchoredPosition = new Vector2(0, yPosition);
        
    }

    IEnumerator CreateConnection()
    {
        GameObject connectionObject = Instantiate(treeConnectionPrefab, treeContent);
        RectTransform connectionRect = connectionObject.GetComponent<RectTransform>();

        RectTransform previousNodeRect = nodeObjects[currentNode-1].GetComponent<RectTransform>();
        RectTransform currentNodeRect = nodeObjects[currentNode].GetComponent<RectTransform>();

        float previousY = previousNodeRect.anchoredPosition.y;
        float currentY = currentNodeRect.anchoredPosition.y;
        float distance = Mathf.Abs(previousY-currentY);

        float connectionHeight = distance - (previousNodeRect.rect.height / 2f) - (currentNodeRect.rect.height / 2f);
        connectionHeight = Mathf.Min(100f, connectionHeight);
        connectionRect.sizeDelta = new Vector2(connectionHeight*2, connectionHeight);

        float middleY = (previousY + currentY) / 2f;
        connectionRect.anchoredPosition = new Vector2(0, middleY);

        TreeConnection connection = connectionObject.GetComponent<TreeConnection>();

        yield return StartCoroutine(connection.FadeIn());
    }
}
