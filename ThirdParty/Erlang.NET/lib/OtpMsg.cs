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
using System.IO;
using System.Net;

namespace Erlang.NET
{
    /**
     * <p>
     * Provides a carrier for Erlang messages.
     * </p>
     * 
     * <p>
     * Instances of this class are created to package header and payload information
     * in received Erlang messages so that the recipient can obtain both parts with
     * a single call to {@link OtpMbox#receiveMsg receiveMsg()}.
     * </p>
     * 
     * <p>
     * The header information that is available is as follows: <lu>
     * <li> a tag indicating the type of message
     * <li> the intended recipient of the message, either as a
     * {@link OtpErlangPid pid} or as a String, but never both.
     * <li> (sometimes) the sender of the message. Due to some eccentric
     * characteristics of the Erlang distribution protocol, not all messages have
     * information about the sending process. In particular, only messages whose tag
     * is {@link OtpMsg#regSendTag regSendTag} contain sender information. </lu>
     * 
     * <p>
     * Message are sent using the Erlang external format (see separate
     * documentation). When a message is received and delivered to the recipient
     * {@link OtpMbox mailbox}, the body of the message is still in this external
     * representation until {@link #getMsg getMsg()} is called, at which point the
     * message is decoded. A copy of the decoded message is stored in the OtpMsg so
     * that subsequent calls to {@link #getMsg getMsg()} do not require that the
     * message be decoded a second time.
     * </p>
     */
    public class OtpMsg
    {
        public const int linkTag = 1;
        public const int sendTag = 2;
        public const int exitTag = 3;
        public const int unlinkTag = 4;
        /* public const int nodeLinkTag = 5; */
        public const int regSendTag = 6;
        /* public const int groupLeaderTag = 7; */
        public const int exit2Tag = 8;

        protected int tag; // what type of message is this (send, link, exit etc)
        protected OtpInputStream paybuf;
        protected OtpErlangObject payload;

        protected OtpErlangPid from;
        protected OtpErlangPid to;
        protected String toName;

        // send has receiver pid but no sender information
        internal OtpMsg(OtpErlangPid to, OtpInputStream paybuf)
        {
            tag = sendTag;
            from = null;
            this.to = to;
            toName = null;
            this.paybuf = paybuf;
            payload = null;
        }

        // send has receiver pid but no sender information
        internal OtpMsg(OtpErlangPid to, OtpErlangObject payload)
        {
            tag = sendTag;
            from = null;
            this.to = to;
            toName = null;
            paybuf = null;
            this.payload = payload;
        }

        // send_reg has sender pid and receiver name
        internal OtpMsg(OtpErlangPid from, String toName, OtpInputStream paybuf)
        {
            tag = regSendTag;
            this.from = from;
            this.toName = toName;
            to = null;
            this.paybuf = paybuf;
            payload = null;
        }

        // send_reg has sender pid and receiver name
        internal OtpMsg(OtpErlangPid from, String toName, OtpErlangObject payload)
        {
            tag = regSendTag;
            this.from = from;
            this.toName = toName;
            to = null;
            paybuf = null;
            this.payload = payload;
        }

        // exit (etc) has from, to, reason
        internal OtpMsg(int tag, OtpErlangPid from, OtpErlangPid to, OtpErlangObject reason)
        {
            this.tag = tag;
            this.from = from;
            this.to = to;
            paybuf = null;
            payload = reason;
        }

        // special case when reason is an atom (i.e. most of the time)
        internal OtpMsg(int tag, OtpErlangPid from, OtpErlangPid to, String reason)
        {
            this.tag = tag;
            this.from = from;
            this.to = to;
            paybuf = null;
            payload = new OtpErlangAtom(reason);
        }

        // other message types (link, unlink)
        internal OtpMsg(int tag, OtpErlangPid from, OtpErlangPid to)
        {
            // convert TT-tags to equiv non-TT versions
            if (tag > 10)
            {
                tag -= 10;
            }

            this.tag = tag;
            this.from = from;
            this.to = to;
        }

        /**
         * Get the payload from this message without deserializing it.
         * 
         * @return the serialized Erlang term contained in this message.
         * 
         */
        internal OtpInputStream getMsgBuf()
        {
            return paybuf;
        }

        /**
         * <p>
         * Get the type marker from this message. The type marker identifies the
         * type of message. Valid values are the ``tag'' constants defined in this
         * class.
         * </p>
         * 
         * <p>
         * The tab identifies not only the type of message but also the content of
         * the OtpMsg object, since different messages have different components, as
         * follows:
         * </p>
         * 
         * <ul>
         * <li> sendTag identifies a "normal" message. The recipient is a
         * {@link OtpErlangPid Pid} and it is available through {@link
         * #getRecipientPid getRecipientPid()}. Sender information is not available.
         * The message body can be retrieved with {@link #getMsg getMsg()}. </li>
         * 
         * <li> regSendTag also identifies a "normal" message. The recipient here is
         * a String and it is available through {@link #getRecipientName
         * getRecipientName()}. Sender information is available through
         * #getSenderPid getSenderPid()}. The message body can be retrieved with
         * {@link #getMsg getMsg()}. </li>
         * 
         * <li> linkTag identifies a link request. The Pid of the sender is
         * available, as well as the Pid to which the link should be made. </li>
         * 
         * <li> exitTag and exit2Tag messages are sent as a result of broken links.
         * Both sender and recipient Pids and are available through the
         * corresponding methods, and the "reason" is available through
         * {@link #getMsg getMsg()}. </li>
         * </ul>
         */
        public int type()
        {
            return tag;
        }

        /**
         * <p>
         * Deserialize and return a new copy of the message contained in this
         * OtpMsg.
         * </p>
         * 
         * <p>
         * The first time this method is called the actual payload is deserialized
         * and the Erlang term is created. Calling this method subsequent times will
         * not cuase the message to be deserialized additional times, instead the
         * same Erlang term object will be returned.
         * </p>
         * 
         * @return an Erlang term.
         * 
         * @exception OtpErlangDecodeException
         *                    if the byte stream could not be deserialized.
         * 
         */
        public OtpErlangObject getMsg()
        {
            if (payload == null)
            {
                payload = paybuf.read_any();
            }
            return payload;
        }

        /**
         * <p>
         * Get the name of the recipient for this message.
         * </p>
         * 
         * <p>
         * Messages are sent to Pids or names. If this message was sent to a name
         * then the name is returned by this method.
         * </p>
         * 
         * @return the name of the recipient, or null if the recipient was in fact a
         *         Pid.
         */
        public String getRecipientName()
        {
            return toName;
        }

        /**
         * <p>
         * Get the Pid of the recipient for this message, if it is a sendTag
         * message.
         * </p>
         * 
         * <p>
         * Messages are sent to Pids or names. If this message was sent to a Pid
         * then the Pid is returned by this method. The recipient Pid is also
         * available for link, unlink and exit messages.
         * </p>
         * 
         * @return the Pid of the recipient, or null if the recipient was in fact a
         *         name.
         */
        public OtpErlangPid getRecipientPid()
        {
            return to;
        }

        /**
         * <p>
         * Get the name of the recipient for this message, if it is a regSendTag
         * message.
         * </p>
         * 
         * <p>
         * Messages are sent to Pids or names. If this message was sent to a name
         * then the name is returned by this method.
         * </p>
         * 
         * @return the Pid of the recipient, or null if the recipient was in fact a
         *         name.
         */
        public Object getRecipient()
        {
            if (toName != null)
            {
                return toName;
            }
            return to;
        }

        /**
         * <p>
         * Get the Pid of the sender of this message.
         * </p>
         * 
         * <p>
         * For messages sent to names, the Pid of the sender is included with the
         * message. The sender Pid is also available for link, unlink and exit
         * messages. It is not available for sendTag messages sent to Pids.
         * </p>
         * 
         * @return the Pid of the sender, or null if it was not available.
         */
        public OtpErlangPid getSenderPid()
        {
            return from;
        }
    }
}

