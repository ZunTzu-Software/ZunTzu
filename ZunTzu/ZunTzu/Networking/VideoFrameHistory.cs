// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZunTzu.Networking {

	internal struct VideoFrame {
		public VideoFrame(byte id, byte[] data) {
			Id = id;
			Data = data;
		}
		public byte Id;
		public byte[] Data;
	}

	internal class OutboundVideoFrameHistory {
		public byte AddFrame(byte[] frameData) {
			byte frameId = 0;
			if (_history.Count > 0)
			{
				VideoFrame previousFrame = _history.Last.Value;
				frameId = (byte)(previousFrame.Id + 1);
			}

			// clear the history every 256 frames
			// as a consequence, the equivalent of a MPEG "I" frame will be emitted 
			if (frameId == 0)
			{
				_history.Clear();
				_noAckReceivedYet = true;
			}

			_history.AddLast(new VideoFrame(frameId, frameData));
			return frameId;
		}

		public byte? LatestAckedFrameId {
			get {
				if (_noAckReceivedYet) return null;
				return _history.First.Value.Id;
			}
		}

		public byte[] LatestAckedFrameData {
			get {
				if (_noAckReceivedYet) throw new InvalidOperationException();
				return _history.First.Value.Data;
			}
		}

		public void AckFrame(byte frameId) {
			if (_history.Count > 0 && _history.Last.Value.Id >= frameId)
			{
				_noAckReceivedYet = false;
				while (_history.First.Value.Id < frameId)
					_history.RemoveFirst();
			}
		}

		LinkedList<VideoFrame> _history = new LinkedList<VideoFrame>();
		bool _noAckReceivedYet = true;
	}

	internal class InboundVideoFrameHistory {
		public void AddFrame(byte frameId, byte[] frameData) {
			history.AddLast(new VideoFrame(frameId, frameData));
		}

		public byte[] GetFrameData(byte frameId) {
			foreach(VideoFrame frame in history)
				if(frame.Id == frameId)
					return frame.Data;
			return null;
		}

		public void ClearHistoryUntilThisFrame(byte frameId) {
			foreach(VideoFrame frame in history) {
				if(frame.Id == frameId) {
					while(history.First.Value.Id != frameId)
						history.RemoveFirst();
					break;
				}
			}
		}

		private LinkedList<VideoFrame> history = new LinkedList<VideoFrame>();
	}
}
