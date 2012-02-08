// Copyright (c) 2011 XU, Jiang Yan <me@jxu.me>, University of Florida
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.ProvisionSupport {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using ProtoBuf;

    public class ChunkMapSerializer {
        public static void Serialize(string filePath, ChunkMapDto chunkMap) {
            using (var stream = File.OpenWrite(filePath)) {
                Serialize(stream, chunkMap);
            }
        }

        public static ChunkMapDto Deserialize(string protoFile) {
            using (var stream = File.OpenRead(protoFile)) {
                return Deserialize(stream);
            }
        }

        public static void Serialize(Stream stream, ChunkMapDto chunkMap) {
            Serializer.Serialize<ChunkMapDto>(stream, chunkMap);
        }

        public static ChunkMapDto Deserialize(Stream stream) {
            return Serializer.Deserialize<ChunkMapDto>(stream);
        }
    }
}
