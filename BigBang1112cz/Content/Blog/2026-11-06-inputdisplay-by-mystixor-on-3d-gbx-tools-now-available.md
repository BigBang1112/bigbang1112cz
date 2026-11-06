---
title: InputDisplay by Mystixor on 3D Gbx Tools now available for everyone, and why it took so long
published: 2026-11-06 21:00
image: /img/inputdisplay.jpg
---

Possibly the most asked feature for [3D Gbx Tools](https://3d.gbx.tools/) in both TMF and TM2 communities is finally here, but it was not meant to be for everyone.

Roughly starting 2023, I was thinking about how to integrate the idea of a Supporter features into the old Gbx Web Tools project, but I wasn't quite sure how to do it. It was always quite difficult for me to setup and find reasonable features on top. As I was spending 6 hours a day developing my Gbx projects, I was kinda in need to find some justification of why I keep doing this. So I settled down on a simple Donate button at that time, as it was just the simplest and I didn't have the capacity to focus on it further. But when I made that original Replay Viewer and Clip Input 2, I got two possible and realistic ideas.

First one is higher limits on serversided processing. When some Gbx tool would run on a server, it puts some extra processing pressure on it. This doesn't have to reflect on the cost I would pay (depends on the server billing type), but it can still take some of the provided CPU/RAM bandwidth, and in some cases even endanger the server stability. It can be caused for example by hundreds of files processed at once *by different people* or malformed Gbx files that would allocate gigabytes of RAM. This is why I already implement strict limits in [Revalidate](https://revalidate.gbx.tools/) for example. But the idea was to extend it much further for supporters. I still think this is valid usecase of a supporter feature and I will eventually introduce it to my services, but never as a downgrade - I will initially release features with the limitations first... as you will read later.

Second idea was having some clientside features that would show up only for supporters, as the apps I develop are usually meant to run fully in your browser or on your desktop. This is more of a formality kind of support, because anything you give to client can be cheesed, and that was really the point of it. For people that are curious or interested in development, they are free to overcome the restriction and I won't be mad. For people that just want good things on top without thinking about them, they can become supporters. 

Long story short, this is how the idea of supporter-only input visualizer came up. Supporters would receive an extra input display via some secret cookie variable, while others could visualize inputs already with my other tool Clip Input, so no one was technically scammed. Nerds and curious guys could still find a way to use it for free, but it would reward them with a few fun skills on top, which is also what I support. For the sake of raw input analysis, I still kept a simple input list somewhere though, which was mainly useful for some respawn management.

## Reality kicked in

But both ideas were still way too far from reality to do, as you need to setup not only a proper authentication system, but also a solid payment gateway, and process all the legal things related to that. While I was continuing with other projects, with a couple of donations every year, I just didn't feel like I need to focus on it...

In late 2024, I think about how to integrate supporter features to Nations Converter, but nothing else than increased limits of future serversided conversion comes to my mind. So I setup Liberapay and call it a day, as again, it was the simplest to setup for a team split of donations.

In 2025, it's time to release 3D Gbx Tools, and I'm intentionally not working on the input visualizer, because I still couldn't figure out authentication and payment gateway, and I just felt like I had to solve other issues with the project. I didn't even use much of AI to write the project, definitely not the mesh import part and its shaders, those I did all with manual analysis. Most of the issues I didn't even solve to this day. I just put out an input list out as some form of input explanation, and I still don't know if it's even a good feature or not. I received a lot of generous donations from players regarding 3D Gbx Tools, so I just became uncertainly frozen with the input visualizer idea and hoped nobody would talk about it.

But a lot of players indeed did question it. "The input list exists, but would you please add the input visualizer?" I always answered: you have Clip Input already, go use that. "But this would be convenient and useful too." And I don't blame them, it's a thing that comes to every Trackmania player's mind since 2021, inputs, input display. Checkpoint visualizer is useful, but it's just one piece of analysis, and I always understood that.

People didn't question the issues the site currently had, most of the questions were related to either two things:

- TM2020 support
- Input display

Though I was still stuck with an idea I cannot explain without feeling weird about it. So I just briefly explain them that it's meant to be a future supporter feature, and usually that was a good enough answer.

## They really went for it

The same Mystixor from the previous blog post comes to me that he's interested to make the input display. I'm just not sure about it, I explain him that this was meant to be a supporter feature. And he still went for it, made it all by himself, and [opened a pull request on the GitHub page](https://github.com/BigBang1112/gbx-tools-3d/pull/8). He was fully noted that this may not be a feature everyone could have access to. So I get that weird feeling again, and just need to decide that he should be the one that will have permanent free support for the input visualizer.

That pull request sits there since November 2025. I'm glad no one is really asking anything about it, and I'm still focusing on the missing parts and other projects without really thinking about the supporter aspect.

Some months later, I receive a DM that the whole 3D Gbx Tools system has been deployed to the Arena Community website. The shaders are screwed. Their logo is embedded into the 3D view. I try to load replays from other environments than Stadium, they are just not there, they imported only the Stadium.pak contents into the setup. And in the bottom left corner, of course I see: input display in white color, clearly vibecoded piece that was added there with possibly the cheapest LLM possible, cuz it just didn't fit at all.

Did they clone the website only to add this input display? Very likely, there's no other explanation, as after a few exchanged DMs, they removed the logo and the only left thing there in comparison to my site was the input display. They also added a missing link to their source code, which was needed to follow the GNU AGPL license of 3D Gbx Tools.

Some kind of features would be perfect opportunity for a supporter role, or a perfect opportunity for a bit of backlash, or an odd response of a weird clone just to add it there. Today, you can just find a way around it.

## The future of software supporter features

How are we supposed to add supporter-only features to a locally-running software nowadays? If someone doesn't like something, they can clone it without a lot of knowledge and add that feature for free for themselves....

The answer is **service**. If the service is good enough and only requires a quick payment, it can not only be more satisfying to use than any possible clone, but it can also sometimes be cheaper to donate for it than using an AI subscription to add those features on scale and distribute them for free in other ways. If your service is bad, people will just clone the hell out of it.

Input display behind a supporter role could work, because setting up 3D Gbx Tools from scratch is not as simple. But with AI and a miniature guideline in README, it's simple enough to make it run locally. And that's also the service you receive: someone hosts it for you and you don't have to set it up yourself.

Maybe there's a bit of hope that hosting something today 24/7 is way more expensive than a few years ago? What do I mean by hope? As long as someone can host like this or have plenty of space next to their other web projects, why they wouldn't just host the perfect 3D Gbx Tools themselves? Even if it was closed source since the beginning, who's stopping someone else to clone it and reverse engineer it from WebAssembly, make all the supporter quirks available to everyone? The only bottleneck left is the token quota, a bit of money, the place where you host it, and *its limits again*.

I just feel like supporter features in software are becoming kind of more annoying each year and introduce reasons why people should clone you. You are limiting the best service experience from people in the age where people try to bring their smooth experience back, and it's easier than ever to bring it back.

Supporter features don't have a good future on clientside applications. You will have to make them feel "on-top", or harshly lock them behind a backend and make people feel desired to clone the service and make it better themselves. 

And I don't think I can provide the good service for 3D Gbx Tools for a while due to other projects I'm working on or want to work on.

This is why input display is now available for everyone and for free right now.

## The future of 3D Gbx Tools

Thank you Mystixor for taking your time to contribute to the project with something that will be useful and helpful for everyone. I no longer have to feel weird about wanting some money from a site that copies assets from Nadeo, and you deserve the maximum credit for the implementation.

[You can now freely view input visuals in a browser from any device that supports it.](https://3d.gbx.tools/)

I'll keep on being busy as usual! The project has a lot of render problems still, and I feel like with the tooling we have today, it's best to rework it from scratch using a custom-made WebGPU wrapper which will be taught from the game shaders itself that can be easily extracted nowadays. That way I can drop away from three.js limitations and have full control over the performance, and learn something along the way, as I won't just vibecode... I'll vibecode, review, and integrate with frameworks I already know, like Blazor! Having a clue of how something works is still important to me.

This rework will silently start in 2027, including the TM2020 part, stay tuned.

## Links

- [3D Gbx Tools](https://3d.gbx.tools/)
- Mystixor's [X](https://x.com/Mystixor) and [YouTube](https://www.youtube.com/@Mystixor)
- [GameBox Sandbox Discord Server](https://discord.gbx.tools)