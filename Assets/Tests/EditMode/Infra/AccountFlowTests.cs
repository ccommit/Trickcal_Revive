using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

using TrickcalRevive.Infra;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.Infra.Tests
{
    // 파일 IO가 실제로 도는지 확인하는 통합 테스트 — 로그인->로비->로그아웃이
    // "동작하는 전환"인지는 Infra가 진짜 파일을 쓰고 읽어야 검증된다.
    public sealed class AccountFlowTests
    {
        private string tempRoot;
        private JsonFileRepository files;
        private SessionService session;
        private AccountAuthRepository accountAuth;
        private SaveManager saveManager;
        private PlayerDataRepository playerData;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "TrickcalReviveTest_" + Guid.NewGuid());
            files = new JsonFileRepository(tempRoot);
            session = new SessionService(files);
            accountAuth = new AccountAuthRepository(files, new PlaintextPasswordHasher());
            saveManager = new SaveManager(files, session);
            playerData = new PlayerDataRepository(files, session, saveManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }

        [Test]
        public void 회원가입_로그인_로비조회_로그아웃_전체_흐름이_파일에_실제로_반영된다()
        {
            var auth = new GameObject("AuthController").AddComponent<AuthController>();
            var settings = new GameObject("SettingsController").AddComponent<SettingsController>();
            auth.Configure(accountAuth, session);
            settings.Configure(session);

            Assert.That(auth.SignUp("player1", "pw1234", "닉네임"), Is.EqualTo(AuthResult.Success));
            Assert.That(session.GetSession(), Is.EqualTo("player1"), "가입 직후 세션이 저장돼야 한다");

            var account = playerData.GetAccount();
            Assert.That(account, Is.Not.Null);
            Assert.That(account.Nickname, Is.EqualTo("닉네임"));
            Assert.That(account.Stamina, Is.EqualTo(21), "20 + player_level(1)");

            var currencies = playerData.GetCurrencies();
            Assert.That(currencies, Has.Count.EqualTo(4));

            settings.Logout();
            Assert.That(session.GetSession(), Is.Null, "로그아웃 후 세션이 지워져야 한다");

            var result = auth.Login("player1", "pw1234");
            Assert.That(result, Is.EqualTo(AuthResult.Success));
            Assert.That(session.GetSession(), Is.EqualTo("player1"), "재로그인하면 세션이 다시 생겨야 한다");

            UnityEngine.Object.DestroyImmediate(auth.gameObject);
            UnityEngine.Object.DestroyImmediate(settings.gameObject);
        }

        [Test]
        public void 틀린_비밀번호는_로그인을_거부한다()
        {
            var auth = new GameObject("AuthController").AddComponent<AuthController>();
            auth.Configure(accountAuth, session);
            auth.SignUp("player2", "correct", "닉");
            session.ClearSession();

            var result = auth.Login("player2", "wrong");

            Assert.That(result, Is.EqualTo(AuthResult.WrongPassword));
            Assert.That(session.GetSession(), Is.Null);

            UnityEngine.Object.DestroyImmediate(auth.gameObject);
        }

        [Test]
        public void 존재하지_않는_아이디_로그인은_NotFound다()
        {
            var auth = new GameObject("AuthController").AddComponent<AuthController>();
            auth.Configure(accountAuth, session);

            var result = auth.Login("nobody", "pw");

            Assert.That(result, Is.EqualTo(AuthResult.NotFound));

            UnityEngine.Object.DestroyImmediate(auth.gameObject);
        }

        [Test]
        public void 이미_있는_아이디로_가입하면_LoginIdTaken이다()
        {
            var auth = new GameObject("AuthController").AddComponent<AuthController>();
            auth.Configure(accountAuth, session);
            auth.SignUp("dup", "pw", "닉");

            var result = auth.SignUp("dup", "other", "닉2");

            Assert.That(result, Is.EqualTo(AuthResult.LoginIdTaken));

            UnityEngine.Object.DestroyImmediate(auth.gameObject);
        }
    }
}
