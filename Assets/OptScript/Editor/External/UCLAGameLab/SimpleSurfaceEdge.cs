/****************************************************************************
Copyright (c) 2017, Jonathan Cecil and UCLA Game Lab
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
this list of conditions and the following disclaimer in the documentation
and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*****************************************************************************/

using UnityEngine;
using System.Collections.Generic;

/***
* MC_SimpleSurfaceEdge
*	class to read pixel data and create outline edges around opaque pixel areas.
*
***/

class MC_SimpleSurfaceEdge
{
    private const float versionNumber = 0.8f;

    private Color[] _pixels; // the original pixel data from the image
    private int _imageHeight;
    private int _imageWidth;

    public List<MC_Edge> edges;
    public List<MC_EdgeLoop> edgeLoops;
    public List<MC_Vertex> vertices;

    public MC_SimpleSurfaceEdge(Color[] pixels, int imageWidth, int imageHeight, float threshold)
    {
        _pixels = pixels;
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;

        edgeLoops = new List<MC_EdgeLoop>();
        edges = new List<MC_Edge>();
        vertices = new List<MC_Vertex>();

        float uvWidth = _imageWidth;
        float uvHeight = _imageHeight;

        for (int x = 0; x < _imageWidth; x++)
        {
            float uvX = x + 0.5f;
            for (int y = 0; y < _imageHeight; y++)
            {
                float uvY = y + 0.5f;
                // get the first pixel
                Color pixel = _pixels[x + (_imageWidth * y)];
                float pixelAlpha = pixel.a;

                // only continue if the current pixel is opaque
                if (pixelAlpha < threshold) continue;

                // set up values for other possible pixel values
                float pixelAboveAlpha = 0.0f;
                float pixelBelowAlpha = 0.0f;
                float pixelRightAlpha = 0.0f;
                float pixelLeftAlpha = 0.0f;
                float pixelAboveRightAlpha = 0.0f;
                float pixelAboveLeftAlpha = 0.0f;
                float pixelBelowRightAlpha = 0.0f;

                // check x area, then the y. 
                if (x > 0 && x < _imageWidth - 1)
                {
                    if (y > 0 && y < _imageHeight - 1)
                    {
                        Color pixelAbove = _pixels[x + (_imageWidth * (y + 1))];
                        pixelAboveAlpha = pixelAbove.a;
                        Color pixelBelow = _pixels[x + (_imageWidth * (y - 1))];
                        pixelBelowAlpha = pixelBelow.a;
                        Color pixelRight = _pixels[x + 1 + (_imageWidth * y)];
                        pixelRightAlpha = pixelRight.a;
                        Color pixelLeft = _pixels[x - 1 + (_imageWidth * y)];
                        pixelLeftAlpha = pixelLeft.a;

                        Color pixelAboveRight = _pixels[x + 1 + (_imageWidth * (y + 1))];
                        pixelAboveRightAlpha = pixelAboveRight.a;
                        Color pixelAboveLeft = _pixels[x - 1 + (_imageWidth * (y + 1))];
                        pixelAboveLeftAlpha = pixelAboveLeft.a;
                        Color pixelBelowRight = _pixels[x + 1 + (_imageWidth * (y - 1))];
                        pixelBelowRightAlpha = pixelBelowRight.a;

                    }
                    else if (y == 0)
                    {
                        Color pixelAbove = _pixels[x + (_imageWidth * (y + 1))];
                        pixelAboveAlpha = pixelAbove.a;
                        Color pixelRight = _pixels[x + 1 + (_imageWidth * y)];
                        pixelRightAlpha = pixelRight.a;
                        Color pixelLeft = _pixels[x - 1 + (_imageWidth * y)];
                        pixelLeftAlpha = pixelLeft.a;

                        Color pixelAboveRight = _pixels[x + 1 + (_imageWidth * (y + 1))];
                        pixelAboveRightAlpha = pixelAboveRight.a;
                        Color pixelAboveLeft = _pixels[x - 1 + (_imageWidth * (y + 1))];
                        pixelAboveLeftAlpha = pixelAboveLeft.a;
                    }
                    else if (y == _imageHeight - 1)
                    {
                        Color pixelBelow = _pixels[x + (_imageWidth * (y - 1))];
                        pixelBelowAlpha = pixelBelow.a;
                        Color pixelRight = _pixels[x + 1 + (_imageWidth * y)];
                        pixelRightAlpha = pixelRight.a;
                        Color pixelLeft = _pixels[x - 1 + (_imageWidth * y)];
                        pixelLeftAlpha = pixelLeft.a;

                        Color pixelBelowRight = _pixels[x + 1 + (_imageWidth * (y - 1))];
                        pixelBelowRightAlpha = pixelBelowRight.a;
                    }
                    else
                    {
                        Debug.Log("SimpleSurfaceEdge:: error constructing pixel values, misinterpreted y values. Please create a new issue at https://github.com/uclagamelab/MeshCreator/issues.");
                    }
                }
                else if (x == 0)
                {
                    if (y > 0 && y < _imageHeight - 1)
                    {
                        Color pixelAbove = _pixels[x + (_imageWidth * (y + 1))];
                        pixelAboveAlpha = pixelAbove.a;
                        Color pixelBelow = _pixels[x + (_imageWidth * (y - 1))];
                        pixelBelowAlpha = pixelBelow.a;
                        Color pixelRight = _pixels[x + 1 + (_imageWidth * y)];
                        pixelRightAlpha = pixelRight.a;

                        Color pixelAboveRight = _pixels[x + 1 + (_imageWidth * (y + 1))];
                        pixelAboveRightAlpha = pixelAboveRight.a;
                        Color pixelBelowRight = _pixels[x + 1 + (_imageWidth * (y - 1))];
                        pixelBelowRightAlpha = pixelBelowRight.a;
                    }
                    else if (y == 0)
                    {
                        Color pixelAbove = _pixels[x + (_imageWidth * (y + 1))];
                        pixelAboveAlpha = pixelAbove.a;
                        Color pixelRight = _pixels[x + 1 + (_imageWidth * y)];
                        pixelRightAlpha = pixelRight.a;

                        Color pixelAboveRight = _pixels[x + 1 + (_imageWidth * (y + 1))];
                        pixelAboveRightAlpha = pixelAboveRight.a;
                    }
                    else if (y == _imageHeight - 1)
                    {
                        Color pixelBelow = _pixels[x + (_imageWidth * (y - 1))];
                        pixelBelowAlpha = pixelBelow.a;
                        Color pixelRight = _pixels[x + 1 + (_imageWidth * y)];
                        pixelRightAlpha = pixelRight.a;

                        Color pixelBelowRight = _pixels[x + 1 + (_imageWidth * (y - 1))];
                        pixelBelowRightAlpha = pixelBelowRight.a;
                    }
                    else
                    {
                        Debug.Log("SimpleSurfaceEdge:: error constructing pixel values, misinterpreted y values.  Please create a new issue at https://github.com/uclagamelab/MeshCreator/issues.");
                    }

                }
                else if (x == _imageWidth - 1)
                {
                    if (y > 0 && y < _imageHeight - 1)
                    {
                        Color pixelAbove = _pixels[x + (_imageWidth * (y + 1))];
                        pixelAboveAlpha = pixelAbove.a;
                        Color pixelBelow = _pixels[x + (_imageWidth * (y - 1))];
                        pixelBelowAlpha = pixelBelow.a;
                        Color pixelLeft = _pixels[x - 1 + (_imageWidth * y)];
                        pixelLeftAlpha = pixelLeft.a;

                        Color pixelAboveLeft = _pixels[x - 1 + (_imageWidth * (y + 1))];
                        pixelAboveLeftAlpha = pixelAboveLeft.a;
                    }
                    else if (y == 0)
                    {
                        Color pixelAbove = _pixels[x + (_imageWidth * (y + 1))];
                        pixelAboveAlpha = pixelAbove.a;
                        Color pixelLeft = _pixels[x - 1 + (_imageWidth * y)];
                        pixelLeftAlpha = pixelLeft.a;

                        Color pixelAboveLeft = _pixels[x - 1 + (_imageWidth * (y + 1))];
                        pixelAboveLeftAlpha = pixelAboveLeft.a;
                    }
                    else if (y == _imageHeight - 1)
                    {
                        Color pixelBelow = _pixels[x + (_imageWidth * (y - 1))];
                        pixelBelowAlpha = pixelBelow.a;
                        Color pixelLeft = _pixels[x - 1 + (_imageWidth * y)];
                        pixelLeftAlpha = pixelLeft.a;

                    }
                    else
                    {
                        Debug.Log("SimpleSurfaceEdge:: error constructing pixel values, misinterpreted y values.  Please create a new issue at https://github.com/uclagamelab/MeshCreator/issues.");
                    }
                }

                // try the up facing case
                if (pixelAlpha >= threshold && pixelAboveAlpha >= threshold)
                {
                    if (pixelAboveRightAlpha < threshold && pixelRightAlpha < threshold)
                    {
                        if (pixelAboveLeftAlpha >= threshold || pixelLeftAlpha >= threshold)
                        {
                            // add the vertical edge
                            MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x, y + 1, uvX / uvWidth, (uvY + 1) / uvHeight));
                            edges.Add(e);
                        }
                    }
                    else if (pixelAboveLeftAlpha < threshold && pixelLeftAlpha < threshold)
                    {
                        if (pixelAboveRightAlpha >= threshold || pixelRightAlpha >= threshold)
                        {
                            // add the vertical edge
                            MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x, y + 1, uvX / uvWidth, (uvY + 1) / uvHeight));
                            edges.Add(e);
                        }
                    }
                }

                // try the up diagonal case
                if (pixelAlpha >= threshold && pixelAboveRightAlpha >= threshold)
                {
                    if (pixelAboveAlpha < threshold && pixelRightAlpha >= threshold)
                    {
                        // add the up diagonal edge
                        MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x + 1, y + 1, (uvX + 1) / uvWidth, (uvY + 1) / uvHeight));
                        edges.Add(e);
                    }
                    else if (pixelAboveAlpha >= threshold && pixelRightAlpha < threshold)
                    {
                        // add the up diagonal edge
                        MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x + 1, y + 1, (uvX + 1) / uvWidth, (uvY + 1) / uvHeight));
                        edges.Add(e);
                    }
                }

                // try the right facing case
                if (pixelAlpha >= threshold && pixelRightAlpha >= threshold)
                {
                    if (pixelAboveAlpha < threshold && pixelAboveRightAlpha < threshold)
                    {
                        if (pixelBelowAlpha >= threshold || pixelBelowRightAlpha >= threshold)
                        {
                            // add the horizontal edge
                            MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x + 1, y, (uvX + 1) / uvWidth, uvY / uvHeight));
                            edges.Add(e);
                        }
                    }
                    else if (pixelBelowAlpha < threshold && pixelBelowRightAlpha < threshold)
                    {
                        if (pixelAboveAlpha >= threshold || pixelAboveRightAlpha >= threshold)
                        {
                            // add the horizontal edge
                            MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x + 1, y, (uvX + 1) / uvWidth, uvY / uvHeight));
                            edges.Add(e);
                        }
                    }
                }

                // try the down diagonal case
                if (pixelAlpha >= threshold && pixelBelowRightAlpha >= threshold)
                {
                    if (pixelRightAlpha < threshold && pixelBelowAlpha >= threshold)
                    {
                        // add the down diagonal edge
                        MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x + 1, y - 1, (uvX + 1) / uvWidth, (uvY - 1) / uvHeight));
                        edges.Add(e);
                    }
                    else if (pixelRightAlpha >= threshold && pixelBelowAlpha < threshold)
                    {
                        // ad the down diagonal edge
                        MC_Edge e = new MC_Edge(GetVertex(x, y, uvX / uvWidth, uvY / uvHeight), GetVertex(x + 1, y - 1, (uvX + 1) / uvWidth, (uvY - 1) / uvHeight));
                        edges.Add(e);
                    }
                }
            }
        }

        MakeOutsideEdge();
        SimplifyEdge();
    }

    MC_Vertex GetVertex(float x, float y, float u, float v)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            MC_Vertex ver = (MC_Vertex)vertices[i];
            if (ver.x == x && ver.y == y)
            {
                return ver;
            }
        }
        MC_Vertex newver = new MC_Vertex(x, y, u, v);
        vertices.Add(newver);
        return newver;
    }

    public void MergeClosePoints(float mergeDistance)
    {
        foreach (MC_EdgeLoop edgeLoop in edgeLoops)
        {
            edgeLoop.MergeClosePoints(mergeDistance);
        }
    }

    void MakeOutsideEdge()
    {

        // order the edges
        // start first edge loop with the first outside edge
        MC_EdgeLoop currentEdgeLoop = new MC_EdgeLoop(edges[0]);
        edges.RemoveAt(0);
        edgeLoops.Add(currentEdgeLoop);

        while (edges.Count > 0)
        {
            // if the currentEdgeLoop is fully closed make a new edge loop
            if (currentEdgeLoop.IsClosed())
            {
                MC_EdgeLoop nextEdgeLoop = new MC_EdgeLoop(edges[0]);
                //Debug.LogWarning("SimpleSurfaceEdge::MakeOutsideEdge: adding another edge loop, last one was " + currentEdgeLoop.orderedEdges.Count + " edges long");
                //Debug.LogWarning("    this means your image has islands, I hope that's what you want.");
                edges.RemoveAt(0);
                edgeLoops.Add(nextEdgeLoop);
                currentEdgeLoop = nextEdgeLoop;
            }
            // test each edge to see if it fits into the edgeloop
            List<MC_Edge> deleteEdges = new List<MC_Edge>();
            for (int i = 0; i < edges.Count; i++)
            {
                MC_Edge e = edges[i];
                if (currentEdgeLoop.AddEdge(e))
                { // try to add the edge
                    deleteEdges.Add(e);
                }
            }
            // delete the added edges
            for (int i = 0; i < deleteEdges.Count; i++)
            {
                edges.Remove((MC_Edge)deleteEdges[i]);
            }
        }

    }

    void SimplifyEdge()
    {
        foreach (MC_EdgeLoop edgeLoop in edgeLoops)
        {
            edgeLoop.SimplifyEdge();
        }
    }
}

class MC_Vertex
{
    public float x, y;
    public float u, v;
    public MC_Vertex(float _x, float _y, float _u, float _v)
    {
        x = _x;
        y = _y;
        u = _u;
        v = _v;
    }

    /*
    *	GetString() returns a descriptive string about info in this object
    *	Useful for debugging.
    */
    public string GetString()
    {
        return "Vertex(x,y:" + x + "," + y + ", uv:" + u + "," + v + ")";
    }

}

class MC_Edge
{
    public MC_Vertex v1;
    public MC_Vertex v2;
    public bool isShared; // indicate if there are two of these?

    List<MC_Face> attachedFaces;

    public MC_Edge(MC_Vertex _v1, MC_Vertex _v2)
    {
        v1 = _v1;
        v2 = _v2;
        isShared = false;
        attachedFaces = new List<MC_Face>();
    }

    public void AttachFace(MC_Face f)
    {
        attachedFaces.Add(f);
    }

    public bool OtherFaceCentered(MC_Face f)
    {
        if (attachedFaces.Count > 1)
        {
            MC_Face face1 = attachedFaces[0];
            MC_Face face2 = attachedFaces[1];
            if (face1 == null && face2 == f) return true; // faces already deleted????
            else if (face2 == null && face1 == f) return true;
            else if (face1 != null && face1 != f && face1.IsCentered()) return true;
            else if (face2 != null && face2 != f && face2.IsCentered()) return true;
        }
        return false;
    }

    public void SwitchVertices()
    {
        MC_Vertex hold = v1;
        v1 = v2;
        v2 = hold;
    }

    /*
    *	GetString() returns a descriptive string about info in this object
    *	Useful for debugging.
    */
    public string GetString()
    {
        return "Edge (" + v1.GetString() + ", " + v2.GetString() + ", shared:" + isShared + ")";
    }
}

class MC_Face
{
    public MC_Vertex v1, v2, v3;
    public MC_Edge e1, e2, e3;

    public MC_Face(MC_Edge _e1, MC_Edge _e2, MC_Edge _e3)
    {
        e1 = _e1;
        e2 = _e2;
        e3 = _e3;
        e1.AttachFace(this);
        e2.AttachFace(this);
        e3.AttachFace(this);
        v1 = e1.v1;
        v2 = e1.v2;
        if (e2.v1 != v1 && e2.v1 != v2) v3 = e2.v1;
        else v3 = e2.v2;
    }

    public bool ContainsEdge(MC_Edge e)
    {
        if (e1 == e || e2 == e || e3 == e)
        {
            return true;
        }
        return false;
    }

    public bool IsCentered()
    {
        if (e1.isShared && e2.isShared && e3.isShared)
        {
            return true;
        }
        return false;
    }

    public bool IsCenteredCentered()
    {
        if (IsCentered())
        {
            if (e1.OtherFaceCentered(this) && e2.OtherFaceCentered(this) && e3.OtherFaceCentered(this))
            {
                return true;
            }
        }
        return false;
    }

    public MC_Vertex[] GetVertices()
    {
        return new MC_Vertex[] { v1, v2, v3 };
    }
}

// ordered list of edges
class MC_EdgeLoop
{
    public List<MC_Edge> orderedEdges;

    public MC_EdgeLoop(MC_Edge e)
    {
        orderedEdges = new List<MC_Edge>();
        orderedEdges.Add(e);
    }

    public bool AddEdge(MC_Edge e)
    {
        // see if it shares with the last edge
        MC_Vertex lastVertex = ((MC_Edge)orderedEdges[orderedEdges.Count - 1]).v2;
        if (e.v1 == lastVertex)
        { // this is the correct vertex order
            orderedEdges.Add(e);
            return true;
        }
        else if (e.v2 == lastVertex)
        { // incorrect order, switch before adding 
            e.SwitchVertices();
            orderedEdges.Add(e);
            return true;
        }

        // see if it shares with the first edge
        MC_Vertex firstVertex = ((MC_Edge)orderedEdges[0]).v1;
        if (e.v2 == firstVertex)
        { // this is the correct vertex order
            orderedEdges.Insert(0, e);
            return true;
        }
        else if (e.v1 == firstVertex)
        { // incorrect order, switch before adding
            e.SwitchVertices();
            orderedEdges.Insert(0, e);
            return true;
        }
        return false;
    }

    public bool IsClosed()
    {
        if (orderedEdges.Count < 2) return false;
        MC_Vertex lastVertex = orderedEdges[orderedEdges.Count - 1].v2;
        MC_Vertex firstVertex = orderedEdges[0].v1;
        if (firstVertex.x == lastVertex.x && firstVertex.y == lastVertex.y) return true;
        return false;
    }

    public Vector2[] GetVertexList(float scale = 1, float offsetX = 0, float offsetY = 0)
    {
        Vector2[] verts = new Vector2[orderedEdges.Count];
        for (int i = 0; i < orderedEdges.Count; i++)
        {
            MC_Vertex v = ((MC_Edge)orderedEdges[i]).v1;
            verts[i] = new Vector2(v.x + offsetX, v.y + offsetY) * scale;
        }
        return verts;
    }

    public Vector2 GetUVForIndex(int i)
    {
        if (i >= orderedEdges.Count)
        {
            //Debug.Log("got " + i + " index for ordered edge with " + orderedEdges.Count + " elements");
            return new Vector2();
        }
        MC_Vertex v = ((MC_Edge)orderedEdges[i]).v1;
        return new Vector2(v.u, v.v);
    }

    /*
    *	GetString() returns a descriptive string about info in this object
    *	Useful for debugging.
    */
    public string GetString()
    {
        string s = "EdgeLoop with " + orderedEdges.Count + " edges: ";
        for (int i = 0; i < orderedEdges.Count; i++)
        {
            MC_Edge e = (MC_Edge)orderedEdges[i];
            s += e.GetString() + ", ";
        }
        return s;
    }

    /*
    *	SimplifyEdge() searchs for edges in which the shared vertex is a point
    *	on a line between the two outer points.
    */
    public void SimplifyEdge()
    {
        List<MC_Edge> newOrderedEdges = new List<MC_Edge>(); // list to stick the joined edges
        MC_Edge currentEdge = orderedEdges[0];
        for (int i = 1; i < orderedEdges.Count; i++)
        { // start with the second edge for comparison
            MC_Edge testEdge = orderedEdges[i];
            MC_Vertex v1 = currentEdge.v1;
            MC_Vertex v2 = testEdge.v2;
            MC_Vertex sharedPoint = currentEdge.v2;
            if (sharedPoint != testEdge.v1)
            { // oops, bad list, it should be closed by now
                Debug.LogError("Mesh Creator EdgeLoop Error: list is not ordered when simplifying edge.  Please create a new issue at https://github.com/uclagamelab/MeshCreator/issues.");
                return;
            }
            if (v1 == v2)
            {
                Debug.LogError("Mesh Creator EdgeLoop Error: found matching endpoints for a line when simplifying.  Please create a new issue at https://github.com/uclagamelab/MeshCreator/issues.");
                return;
            }
            // determine if sharedPoint is on a line between the two endpoints
            //The point (x3, y3) is on the line determined by (x1, y1) and (x2, y2) if and only if (x3-x1)*(y2-y1)==(x2-x1)*(y3-y1). 
            float slope1 = (sharedPoint.x - v1.x) * (v2.y - v1.y);
            float slope2 = (v2.x - v1.x) * (sharedPoint.y - v1.y);
            if (slope1 == slope2)
            { // combine the two lines into current
                currentEdge.v2 = v2;
            }
            else
            { // there isn't a continuation of line, so add current to new ordered and set current to testEdge
                newOrderedEdges.Add(currentEdge);
                currentEdge = testEdge;
            }
        }
        newOrderedEdges.Add(currentEdge);
        orderedEdges = newOrderedEdges;
    }

    // very simple edge smoothing by comparing distance between adjacent
    // points on edge and merging if close enough
    public void MergeClosePoints(float mergeDistance)
    {
        if (mergeDistance < 0.0f) return;

        List<MC_Edge> newOrderedEdges = new List<MC_Edge>(); // list to stick the joined edges
        //int originalCount = orderedEdges.Count;
        MC_Edge currentEdge = orderedEdges[0];
        for (int i = 1; i < orderedEdges.Count; i++)
        { // start with the second edge for comparison
            MC_Edge testEdge = orderedEdges[i];
            float dist = Vector2.Distance(new Vector2(currentEdge.v1.x, currentEdge.v1.y), new Vector2(testEdge.v2.x, testEdge.v2.y));
            MC_Vertex v2 = testEdge.v2;

            if (dist < mergeDistance)
            { // combine the two lines into current
                currentEdge.v2 = v2;
            }
            else
            { // there isn't a continuation of line, so add current to new ordered and set current to testEdge
                newOrderedEdges.Add(currentEdge);
                currentEdge = testEdge;
            }
        }
        newOrderedEdges.Add(currentEdge);
        orderedEdges = newOrderedEdges;
        /*if (originalCount != orderedEdges.Count)
        {
            Debug.Log("SimpleSurfaceEdge::MergeClosePoints(): trimmed from " + originalCount + " to " + orderedEdges.Count + " edges.");
        }*/
    }
}