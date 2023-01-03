using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Preview.Controller;
using SenseNet.Tests;

namespace Preview.Tests
{
    [TestClass]
    public class PreviewCommentsTests : TestBase
    {
        [TestMethod]
        public void PreviewComment_Get_One()
        {
            var comment = PreviewCommentActions.GetPreviewComments("[" +
                "{ \"id\": \"abc\", \"page\": 1, \"x\": 11, \"y\": 12, \"text\": \"cmmnt\", \"createdBy\": \"usr1\", \"creationDate\": \"2019.02.27. 15:38:00\" }, " +
                "{ \"id\": \"def\", \"page\": 2 }" +
                "]", 1).Single();

            Assert.AreEqual("abc", comment.Id);
            Assert.AreEqual("cmmnt", comment.Text);
            Assert.AreEqual("usr1", comment.CreatedBy);
            Assert.AreEqual(1, comment.Page);
            Assert.AreEqual(11, comment.X);
            Assert.AreEqual(12, comment.Y);
            Assert.AreEqual(new DateTime(2019, 02, 27, 15, 38, 0),  comment.CreationDate);
        }
        [TestMethod]
        public void PreviewComment_Get_All()
        {
            var comments = PreviewCommentActions.GetPreviewComments("[{ \"id\": \"abc\", \"page\": 1 }, " +
                                                             " { \"id\": \"def\", \"page\": 2 }" +
                                                             "]", -1).ToArray();

            Assert.AreEqual(2, comments.Length);
            Assert.AreEqual("abc", comments[0].Id);
            Assert.AreEqual("def", comments[1].Id);
        }
        [TestMethod]
        public void PreviewComment_Get_Page()
        {
            var comments = PreviewCommentActions.GetPreviewComments("[{ \"id\": \"abc\", \"page\": 1 }, " +
                                                             " { \"id\": \"def\", \"page\": 2 }" +
                                                             "]", 2).ToArray();

            Assert.AreEqual("def", comments.Single().Id);
        }
        [TestMethod]
        public void PreviewComment_Get_More()
        {
            var comments = PreviewCommentActions.GetPreviewComments("[{ \"id\": \"abc\", \"page\": 1 }, " +
                                                             " { \"id\": \"def\", \"page\": 2 }," +
                                                             " { \"id\": \"ghi\", \"page\": 2 }," +
                                                             " { \"id\": \"jkl\", \"page\": 3 }," +
                                                             "]", 2).ToArray();

            Assert.AreEqual(2, comments.Length);
            Assert.AreEqual("def", comments[0].Id);
            Assert.AreEqual("ghi", comments[1].Id);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PreviewComment_Add_UserError()
        {
            PreviewCommentActions.AddPreviewComment(null, null, 1, 1, 1, null, out var _);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PreviewComment_Add_PageError()
        {
            PreviewCommentActions.AddPreviewComment(null, "user", -1, -1, -1, null, out var _);
        }

        [TestMethod]
        public void PreviewComment_Add_Empty()
        {
            var comments = PreviewCommentActions.AddPreviewComment(null, "user", 1, 1, 1, null, out var comment);

            Assert.IsNull(comments.Single()["text"].Value<string>());
            Assert.IsNull(comment.Text);
        }
        [TestMethod]
        public void PreviewComment_Add_Long()
        {
            // add a comment that contains more characters than it is allowed
            var comments = PreviewCommentActions.AddPreviewComment(null, "user", 1, 1, 1, new string('*', 501), 
                out var comment);

            Assert.AreEqual(500, comments.Single()["text"].Value<string>().Length);
            Assert.AreEqual(500, comment.Text.Length);
            Assert.IsTrue(comment.Text.StartsWith("*************"));
        }
        [TestMethod]
        public void PreviewComment_Add_New()
        {
            var comments = PreviewCommentActions.AddPreviewComment("[{ \"id\": \"abc\" }]", "user", 11, 12, 13, "def", 
                out var comment);

            Assert.AreEqual(2, comments.Count);
            Assert.AreEqual("def", comments.Last()["text"].Value<string>());
            Assert.AreEqual("def", comment.Text);
            Assert.AreEqual("user", comment.CreatedBy);
            Assert.AreEqual(11, comment.Page);
            Assert.AreEqual(12, comment.X);
            Assert.AreEqual(13, comment.Y);
        }
        [TestMethod]
        public void PreviewComment_Add_Order()
        {
            const string commentsValue =
                "[{ \"id\": \"abc\", \"page\": 1, \"x\": 100, \"y\": 100, \"creationDate\": \"2019.02.28. 8:00:00\" }, " +
                " { \"id\": \"def\", \"page\": 2, \"x\": 30.5, \"y\": 30.5,   \"creationDate\": \"2019.02.28. 9:00:00\" }," +
                " { \"id\": \"ghi\", \"page\": 2, \"x\": 70.5, \"y\": 70.5,   \"creationDate\": \"2019.02.28. 5:00:00\" }," +
                " { \"id\": \"jkl\", \"page\": 2, \"x\": 70, \"y\": 70,   \"creationDate\": \"2019.02.28. 6:00:00\" }," +
                " { \"id\": \"mno\", \"page\": 3, \"x\": 70, \"y\": 70,   \"creationDate\": \"2010.01.01. 1:00:00\" }," +
                "]";

            AssertCommentOrder(2, 20, 20, 1);
            AssertCommentOrder(2, 20, 30, 1);
            AssertCommentOrder(2, 30.5, 30.5, 2);
            AssertCommentOrder(2, 30.7, 30.7, 2);
            AssertCommentOrder(2, 50, 50, 2);
            AssertCommentOrder(2, 70.8, 90, 4);

            void AssertCommentOrder(int page, double x, double y, int expectedIndex)
            {
                var comments = PreviewCommentActions.AddPreviewComment(commentsValue, "user", page, x, y, "commentx", out _);

                Assert.AreEqual(6, comments.Count);
                Assert.AreEqual("commentx", comments[expectedIndex]?["text"]?.Value<string>() ?? string.Empty, 
                    $"Incorrect index. Expected: {expectedIndex}.");
            }
        }

        [TestMethod]
        public void PreviewComment_Delete()
        {
            // empty edge case
            var comments = PreviewCommentActions.DeletePreviewComment(null, null, out var modified);

            Assert.IsNull(comments);
            Assert.IsFalse(modified);

            // no contents
            comments = PreviewCommentActions.DeletePreviewComment("null", "123", out modified);

            Assert.IsNull(comments);
            Assert.IsFalse(modified);

            // empty array
            comments = PreviewCommentActions.DeletePreviewComment("[]", "123", out modified);

            Assert.AreEqual(0, comments.Count);
            Assert.IsFalse(modified);

            // try to delete a nonexisting comment
            comments = PreviewCommentActions.DeletePreviewComment("[ { \"id\": \"123\" } ]", "456", out modified);

            Assert.AreEqual("123", comments.Single()["id"].Value<string>());
            Assert.IsFalse(modified);

            // delete a single comment
            comments = PreviewCommentActions.DeletePreviewComment("[ { \"id\": \"123\" } ]", "123", out modified);

            Assert.IsFalse(comments.Any());
            Assert.IsTrue(modified);

            // delete a comment from a list
            comments = PreviewCommentActions.DeletePreviewComment("[ { \"id\": \"123\" }, { \"id\": \"456\" } ]", "123", out modified);

            Assert.AreEqual("456", comments.Single()["id"].Value<string>());
            Assert.IsTrue(modified);
        }

        //[TestMethod]
        //public void PreviewComment_Integration_Add()
        //{
        //    //TODO: remove this after refreshing the test structure
        //    Assert.Inconclusive();

        //    Test(() =>
        //    {
        //        //-- Prepare
        //        var file = CreateTestFile();

        //        var comment = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment1");

        //        Assert.AreEqual(Identifiers.AdministratorUserId, comment.CreatedBy.Id);
        //    });
        //}
        //[TestMethod]
        //public void PreviewComment_Integration_Get()
        //{
        //    //TODO: remove this after refreshing the test structure
        //    Assert.Inconclusive();

        //    Test(() =>
        //    {
        //        var file = CreateTestFile();

        //        var comment1 = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment1");
        //        var comment2 = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment2");
        //        var comment3 = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment3");

        //        var comments = ((IEnumerable<PreviewComment>) PreviewActions.GetPreviewComments(
        //            Content.Load(file.Id), 1)).ToArray();

        //        Assert.AreEqual("comment1", comment1.Text);
        //        Assert.AreEqual("comment2", comment2.Text);
        //        Assert.AreEqual("comment3", comment3.Text);

        //        Assert.AreEqual("comment1", comments[0].Text);
        //        Assert.AreEqual("comment2", comments[1].Text);
        //        Assert.AreEqual("comment3", comments[2].Text);

        //        Assert.AreEqual("builtin\\admin", comments[0].CreatedBy.Username);
        //    });
        //}
        //[TestMethod]
        //public void PreviewComment_Integration_Delete()
        //{
        //    //TODO: remove this after refreshing the test structure
        //    Assert.Inconclusive();

        //    Test(() =>
        //    {
        //        var file = CreateTestFile();

        //        var comment1 = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment1");
        //        var comment2 = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment2");
        //        var comment3 = (PreviewComment)PreviewActions.AddPreviewComment(Content.Load(file.Id), 1, 1, 1, "comment3");

        //        var result = PreviewActions.DeletePreviewComment(Content.Load(file.Id), comment2.Id);

        //        //TODO: check result
        //        // Assert result

        //        var comments = ((IEnumerable<PreviewComment>)PreviewActions.GetPreviewComments(
        //            Content.Load(file.Id), 1)).ToArray();

        //        Assert.AreEqual(2, comments.Length);
        //        Assert.AreEqual(comment1.Id, comments[0].Id);
        //        Assert.AreEqual(comment3.Id, comments[1].Id);
        //    });
        //}

        /* ============================================================================= Helper methods */

        private static GenericContent CreateTestRoot(bool save = true)
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            if (save)
                node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return node;
        }

        /// <summary>
        /// Creates a file without binary. Name is a GUID if not passed. Parent is a newly created SystemFolder.
        /// </summary>
        private static File CreateTestFile(string name = null, bool save = true)
        {
            return CreateTestFile(CreateTestRoot(), name ?? Guid.NewGuid().ToString(), save);
        }

        /// <summary>
        /// Creates a file without binary under the given parent node.
        /// </summary>
        private static File CreateTestFile(Node parent, string name = null, bool save = true)
        {
            var file = new File(parent) { Name = name ?? Guid.NewGuid().ToString() };
            if (save)
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return file;
        }
    }
}
