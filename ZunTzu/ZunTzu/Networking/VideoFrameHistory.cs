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
		public static VideoFrame None = new VideoFrame(0, null);
	}

	internal class OutboundVideoFrameHistory {
		public byte AddFrame(byte[] frameData) {
			Debug.Assert(history.Count > 0);
			VideoFrame previousFrame = history.Last.Value;
			byte frameId = (previousFrame.Data == null ? (byte) 0 : (byte) (previousFrame.Id + 1));
			history.AddLast(new VideoFrame(frameId, frameData));
			if(history.Count > 16) {
				history.RemoveFirst();
				history.First.Value = VideoFrame.None;
			}
			return frameId;
		}

		public byte OldestFrameId {
			get {
				Debug.Assert(history.Count > 0 && history.First.Value.Data != null);
				return history.First.Value.Id;
			}
		}

		public byte[] OldestFrameData {
			get {
				Debug.Assert(history.Count > 0);
				return history.First.Value.Data;
			}
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

		private LinkedList<VideoFrame> history = new LinkedList<VideoFrame>(new VideoFrame[] { VideoFrame.None });
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
