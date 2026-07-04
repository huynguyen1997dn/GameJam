using UnityEngine;
using UnityEngine.UI;

public class Day1TutorialSpotlightOverlay : MaskableGraphic
{
    private bool _hasCutout;
    private Vector2 _cutoutCenter;
    private Vector2 _cutoutSize;

    public void SetCutout(Vector2 center, Vector2 size)
    {
        _hasCutout = true;
        _cutoutCenter = center;
        _cutoutSize = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
        SetVerticesDirty();
    }

    public void ClearCutout()
    {
        if (!_hasCutout) return;

        _hasCutout = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = rectTransform.rect;
        if (!_hasCutout || _cutoutSize.x <= 0f || _cutoutSize.y <= 0f)
        {
            AddRect(vertexHelper, rect.xMin, rect.yMin, rect.xMax, rect.yMax);
            return;
        }

        float cutoutLeft = Mathf.Clamp(_cutoutCenter.x - _cutoutSize.x * 0.5f, rect.xMin, rect.xMax);
        float cutoutRight = Mathf.Clamp(_cutoutCenter.x + _cutoutSize.x * 0.5f, rect.xMin, rect.xMax);
        float cutoutBottom = Mathf.Clamp(_cutoutCenter.y - _cutoutSize.y * 0.5f, rect.yMin, rect.yMax);
        float cutoutTop = Mathf.Clamp(_cutoutCenter.y + _cutoutSize.y * 0.5f, rect.yMin, rect.yMax);

        if (cutoutLeft >= cutoutRight || cutoutBottom >= cutoutTop)
        {
            AddRect(vertexHelper, rect.xMin, rect.yMin, rect.xMax, rect.yMax);
            return;
        }

        AddRect(vertexHelper, rect.xMin, rect.yMin, cutoutLeft, rect.yMax);
        AddRect(vertexHelper, cutoutRight, rect.yMin, rect.xMax, rect.yMax);
        AddRect(vertexHelper, cutoutLeft, rect.yMin, cutoutRight, cutoutBottom);
        AddRect(vertexHelper, cutoutLeft, cutoutTop, cutoutRight, rect.yMax);
    }

    private void AddRect(VertexHelper vertexHelper, float xMin, float yMin, float xMax, float yMax)
    {
        if (xMax <= xMin || yMax <= yMin) return;

        int startIndex = vertexHelper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(xMin, yMin);
        vertexHelper.AddVert(vertex);

        vertex.position = new Vector3(xMin, yMax);
        vertexHelper.AddVert(vertex);

        vertex.position = new Vector3(xMax, yMax);
        vertexHelper.AddVert(vertex);

        vertex.position = new Vector3(xMax, yMin);
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
