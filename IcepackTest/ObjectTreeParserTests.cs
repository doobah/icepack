using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Icepack;

namespace IcepackTest
{
    public class ObjectTreeParserTests
    {
        private const string VALID_OBJ_TREE_STRING = "[[1,a string],[2,another string]]";
        private const string CORRECTLY_ESCAPED_OBJ_TREE_STRING = "[[1,a string],[2,another \\] string]]";
        private const string INVALID_OBJ_TREE_STRING = "[[1,a string],[2,another string]";
        private const string INCORRECTLY_ESCAPED_OBJ_TREE_STRING = "[[1,a string],[2,another ] string]]";

        [Test]
        public void ValidString()
        {
            List<object> objTree = ObjectTreeParser.Parse(VALID_OBJ_TREE_STRING);

            Assert.IsInstanceOf(typeof(List<object>), objTree[0]);
            List<object> node0 = (List<object>)objTree[0];
            Assert.AreEqual("1", node0[0]);
            Assert.AreEqual("a string", node0[1]);

            Assert.IsInstanceOf(typeof(List<object>), objTree[1]);
            List<object> node1 = (List<object>)objTree[1];
            Assert.AreEqual("2", node1[0]);
            Assert.AreEqual("another string", node1[1]);
        }

        [Test]
        public void CorrectlyEscapedString()
        {
            List<object> objTree = ObjectTreeParser.Parse(CORRECTLY_ESCAPED_OBJ_TREE_STRING);

            Assert.IsInstanceOf(typeof(List<object>), objTree[0]);
            List<object> node0 = (List<object>)objTree[0];
            Assert.AreEqual("1", node0[0]);
            Assert.AreEqual("a string", node0[1]);

            Assert.IsInstanceOf(typeof(List<object>), objTree[1]);
            List<object> node1 = (List<object>)objTree[1];
            Assert.AreEqual("2", node1[0]);
            Assert.AreEqual("another ] string", node1[1]);
        }

        [Test]
        public void InvalidString()
        {
            Assert.Throws<IcepackException>(() => { List<object> objTree = ObjectTreeParser.Parse(INVALID_OBJ_TREE_STRING); });
        }

        [Test]
        public void IncorrectlyEscapedString()
        {
            Assert.Throws<IcepackException>(() => { List<object> objTree = ObjectTreeParser.Parse(INCORRECTLY_ESCAPED_OBJ_TREE_STRING); });
        }
    }
}
