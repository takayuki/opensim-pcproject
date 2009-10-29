/*
 * %CopyrightBegin%
 * 
 * Copyright Takayuki Usui 2009. All Rights Reserved.
 * Copyright Ericsson AB 2000-2009. All Rights Reserved.
 * 
 * The contents of this file are subject to the Erlang Public License,
 * Version 1.1, (the "License"); you may not use this file except in
 * compliance with the License. You should have received a copy of the
 * Erlang Public License along with this software. If not, it can be
 * retrieved online at http://www.erlang.org/.
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
 * the License for the specific language governing rights and limitations
 * under the License.
 * 
 * %CopyrightEnd%
 */
using System;

namespace Erlang.NET
{
    /**
     * Provides a Java representation of Erlang ports.
     */
    [Serializable]
    public class OtpErlangPort : OtpErlangObject
    {
        // don't change this!
        internal static readonly new long serialVersionUID = 4037115468007644704L;

        private readonly String node;
        private readonly int id;
        private readonly int creation;

        /*
         * Create a unique Erlang port belonging to the local node. Since it isn't
         * meaninful to do so, this constructor is private...
         * 
         * @param self the local node.
         * 
         * @deprecated use OtpLocalNode:createPort() instead
         */
        private OtpErlangPort(OtpSelf self)
        {
            OtpErlangPort p = self.createPort();

            id = p.id;
            creation = p.creation;
            node = p.node;
        }

        /**
         * Create an Erlang port from a stream containing a port encoded in Erlang
         * external format.
         * 
         * @param buf
         *                the stream containing the encoded port.
         * 
         * @exception OtpErlangDecodeException
         *                    if the buffer does not contain a valid external
         *                    representation of an Erlang port.
         */
        public OtpErlangPort(OtpInputStream buf)
        {
            OtpErlangPort p = buf.read_port();

            node = p.Node;
            id = p.Id;
            creation = p.Creation;
        }

        /**
         * Create an Erlang port from its components.
         * 
         * @param node
         *                the nodename.
         * 
         * @param id
         *                an arbitrary number. Only the low order 28 bits will be
         *                used.
         * 
         * @param creation
         *                another arbitrary number. Only the low order 2 bits will
         *                be used.
         */
        public OtpErlangPort(String node, int id, int creation)
        {
            this.node = node;
            this.id = id & 0xfffffff; // 28 bits
            this.creation = creation & 0x03; // 2 bits
        }

        /**
         * Get the id number from the port.
         * 
         * @return the id number from the port.
         */
        public int Id
        {
            get { return id; }
        }

        /**
         * Get the creation number from the port.
         * 
         * @return the creation number from the port.
         */
        public int Creation
        {
            get { return creation; }
        }

        /**
         * Get the node name from the port.
         * 
         * @return the node name from the port.
         */
        public String Node
        {
            get { return node; }
        }

        /**
         * Get the string representation of the port. Erlang ports are printed as
         * #Port&lt;node.id&gt;.
         * 
         * @return the string representation of the port.
         */
        public override String ToString()
        {
            return "#Port<" + node + "." + id + ">";
        }

        /**
         * Convert this port to the equivalent Erlang external representation.
         * 
         * @param buf
         *                an output stream to which the encoded port should be
         *                written.
         */
        public override void encode(OtpOutputStream buf)
        {
            buf.write_port(node, id, creation);
        }

        /**
         * Determine if two ports are equal. Ports are equal if their components are
         * equal.
         * 
         * @param o
         *                the other port to compare to.
         * 
         * @return true if the ports are equal, false otherwise.
         */
        public override bool Equals(Object o)
        {
            if (!(o is OtpErlangPort))
            {
                return false;
            }

            OtpErlangPort port = (OtpErlangPort)o;

            return creation == port.creation && id == port.id
            && node.CompareTo(port.node) == 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        protected override int doHashCode()
        {
            OtpErlangObject.Hash hash = new OtpErlangObject.Hash(6);
            hash.combine(Creation);
            hash.combine(id, node.GetHashCode());
            return hash.valueOf();
        }
    }
}
