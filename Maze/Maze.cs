/*
 * Copyright (c) 2009 Takayuki Usui
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS AND CONTRIBUTORS ``AS IS''
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED.IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
 * OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using Erlang.NET;

namespace Maze
{
    /*
     * It provides a naive implementation of ``Recursive backtracker'',
     * which is one of the simplest maze generation algorithms.
     * For details and further discussion:
     *
     * Maze generation algorithm
     * http://en.wikipedia.org/wiki/Maze_generation_algorithm
     *
     */
    public class Cell
    {
        public readonly int X;
        public readonly int Y;
        public bool Left = true;
        public bool Bottom = true;
        public bool Visited = false;

        public Cell(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class Maze
    {
        private readonly int Width;
        private readonly int Height;
        private Cell[,] cell;
        private Stack<Cell> stack = new Stack<Cell>();
        private Random rand = new Random();

        public Cell[,] Cell
        {
            get { return cell; }
        }

        public Maze(int width, int height)
        {
            Width = width;
            Height = height;

            cell = new Cell[Width + 1, Height + 1];

            for (int i = 0; i <= Width; i++)
                for (int j = 0; j <= Height; j++)
                    cell[i, j] = new Cell(i, j);

            for (int i = 0; i <= Width; i++)
            {
                cell[i, Height].Visited = true;
                cell[i, Height].Left = false;
            }
            for (int j = 0; j <= Height; j++)
            {
                cell[Width, j].Visited = true;
                cell[Width, j].Bottom = false;
            }
        }

        private enum Dir { Right, Top, Left, Bottom }

        private Cell Neighbor(Cell current, Dir dir)
        {

            switch (dir)
            {
                case Dir.Right:
                    if (0 <= current.X && current.X < Width)
                        return cell[current.X + 1, current.Y];
                    break;

                case Dir.Left:
                    if (0 < current.X && current.X <= Width)
                        return cell[current.X - 1, current.Y];
                    break;

                case Dir.Top:
                    if (0 <= current.Y && current.Y < Height)
                        return cell[current.X, current.Y + 1];
                    break;

                case Dir.Bottom:
                    if (0 < current.Y && current.Y <= Height)
                        return cell[current.X, current.Y - 1];
                    break;
            }
            return null;
        }

        private bool Visited(Cell current, Dir dir)
        {
            Cell neighbor = Neighbor(current, dir);

            if (neighbor == null)
            {
                return true;
            }
            else
            {
                return neighbor.Visited;
            }
        }

        private bool VisitedAllNeighbors(Cell c)
        {
            return (Visited(c, Dir.Right) && Visited(c, Dir.Top) &&
                Visited(c, Dir.Left) && Visited(c, Dir.Bottom));
        }

        private Cell Open()
        {
            Cell current = cell[0, 0];
            current.Bottom = false;
            cell[Width - 1, Height].Visited = false;
            return current;
        }

        private void Remove(Cell current, Dir dir, Cell neighbor)
        {
            switch (dir)
            {
                case Dir.Right:
                    neighbor.Left = false;
                    break;

                case Dir.Left:
                    current.Left = false;
                    break;

                case Dir.Top:
                    neighbor.Bottom = false;
                    break;

                case Dir.Bottom:
                    current.Bottom = false;
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        public void Generate()
        {
            Cell current = Open();

            stack.Push(current);

            while (0 < stack.Count)
            {
                current.Visited = true;

                if (VisitedAllNeighbors(current))
                {
                    current = stack.Pop();
                }
                else
                {
                    Cell neighbor = null;
                    Maze.Dir dir;
                    do
                    {
                        dir = (Maze.Dir)(rand.Next() % 4);
                        if (!Visited(current, dir))
                        {
                            neighbor = Neighbor(current, dir);
                            break;
                        }
                    } while (true);

                    stack.Push(current);

                    Remove(current, dir, neighbor);
                    current = neighbor;
                }
            }
        }

        public void Show()
        {
            for (int j = Height; 0 <= j; j--)
            {
                for (int i = 0; i <= Width; i++)
                    Console.Write(cell[i, j].Left ? "| " : "  ");
                Console.WriteLine();
                for (int i = 0; i <= Width; i++)
                    Console.Write(cell[i, j].Bottom ? "+-" : "+ ");
                Console.WriteLine();
            }
        }

        public static OtpErlangTuple Load(OtpMbox mbox, OtpErlangPid pid, string script)
        {
            OtpErlangObject body;
            OtpErlangBoolean debug = new OtpErlangBoolean(false);
            OtpErlangObject message;

            if (script.Length <= 65535)
            {
                body = new OtpErlangString(script);
            }
            else
            {
                body = new OtpErlangBinary(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(script));
            }
            message = new OtpErlangTuple(new OtpErlangObject[] { mbox.Self, new OtpErlangAtom("load"), body, debug });

            mbox.send(pid, message);
            OtpErlangTuple reply = (OtpErlangTuple)mbox.receive();
            Console.WriteLine("Load: {0}", reply);

            return reply;
        }
        
        public static void Main(string[] args)
        {
            OtpNode node = new OtpNode("gen");
            OtpMbox mbox = node.createMbox(true);
            OtpErlangObject message = new OtpErlangTuple(new OtpErlangObject[] { mbox.Self, new OtpErlangAtom("new") });
            
            mbox.send("kernel", "pc@3di0050d", message);
            OtpErlangTuple reply = (OtpErlangTuple)mbox.receive();

            OtpErlangPid self = (OtpErlangPid)reply.elementAt(0);
            OtpErlangAtom ok = (OtpErlangAtom)reply.elementAt(1);
            OtpErlangPid pid = (OtpErlangPid)reply.elementAt(2);

            Console.WriteLine("New: {0}", ok);
            if (ok.ToString() != "ok")
            {
                return;
            }

            mbox.link(pid);

            Maze m = new Maze(50, 50);
            m.Generate();

            string script = String.Empty;
            double z = 25.0;
            double w = 5.0;
            double h = 5.0;
            double d = 0.5;

            script += String.Format("/L {{moveto createbox dup <{0},{1},{2}> setsize dup show}} def\n", d, (w + d), h);
            script += String.Format("/B {{moveto createbox dup <{0},{1},{2}> setsize dup show}} def\n", (w + d), d, h);

            foreach (Cell c in m.Cell)
            {
                double x = ((float)c.X) * w + 3.0;
                double y = ((float)c.Y) * w + 3.0;

                if (c.Left)
                {
                    script += String.Format("<{0},{1},{2}> L\n", x, (y + w / 2.0), (z + h / 2.0));
                }
                if (c.Bottom)
                {
                    script += String.Format("<{0},{1},{2}> B\n", (x + w / 2.0), y, (z + h / 2.0));
                }
            }

            Load(mbox, pid, script);

            Console.WriteLine("Hit return key to continue");
            Console.ReadLine();

            mbox.send(pid, new OtpErlangTuple(new OtpErlangObject[] { mbox.Self, new OtpErlangAtom("exit") }));
            reply = (OtpErlangTuple)mbox.receive();

            mbox.close();
            node.close();
        }
    }
}